using Azure.Messaging.ServiceBus;
using AutoFixture;
using GameStore.Api.Shared.Messaging;
using GameStore.Contracts.Orders;
using GameStore.Data;
using GameStore.Data.Models;
using GameStore.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Testcontainers.PostgreSql;
using Testcontainers.ServiceBus;
using GameStore.IntegrationTests.Data;

namespace GameStore.IntegrationTests.Worker;

public class OrdersWorkerTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer postgreContainer = new PostgreSqlBuilder().Build();
    private ServiceBusContainer? serviceBusContainer;
    private readonly Fixture fixture = new();

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        await postgreContainer.StartAsync(CancellationToken);

        // Align with other tests: generate current timestamps for DateTimeOffset values
        fixture.Customize<DateTimeOffset>(o => o.FromFactory(() => DateTimeOffset.UtcNow));

        // Configure Service Bus Emulator with a static config file committed to the repo.
        // The file is copied to the output folder by the test csproj.
        var configFile = Path.Combine(AppContext.BaseDirectory, "Worker", "servicebus.config.json");

        serviceBusContainer = new ServiceBusBuilder()
                .WithAcceptLicenseAgreement(true)
                .WithConfig(configFile)
                .Build();

        await serviceBusContainer.StartAsync(CancellationToken);
    }

    [Fact]
    public async Task Consume_OrderPaid_CompletesOrder()
    {
        // 1) Ensure database is migrated and create an order in Processing state
        var dbConnString = postgreContainer.GetConnectionString();
        var sbConnString = serviceBusContainer!.GetConnectionString();

        var dbOptions = new DbContextOptionsBuilder<GameStoreContext>()
            .UseNpgsql(dbConnString)
            .Options;

        var orderId = Guid.NewGuid();

        await using (var setupCtx = new GameStoreContext(dbOptions))
        {
            await setupCtx.Database.MigrateAsync(CancellationToken);

            var order = fixture.Build<Order>()
                .With(o => o.Id, orderId)
                .With(o => o.Status, OrderStatus.Processing)
                // Ensure we have at least one item with a positive quantity
                .With(o => o.Items, [.. fixture.Build<OrderItem>()
                                            .With(i => i.Quantity, 2)
                                            .CreateMany(1)])
                .Create();

            setupCtx.Orders.Add(order);
            await setupCtx.SaveChangesAsync(CancellationToken);
        }

        // 2) Prepare an interceptor and start the worker host with DI overrides
        var probe = new OrderCompletedInterceptor(orderId);

        using var host = WorkerHostFactory.Build(
            environmentName: "Testing",
            configure: configBuilder =>
            {
                var overrides = new Dictionary<string, string?>
                {
                    ["ConnectionStrings:GameStoreDB"] = dbConnString,
                    ["ConnectionStrings:serviceBus"] = sbConnString
                };
                configBuilder.AddInMemoryCollection(overrides);
            },
            testOverrides: services =>
            {
                // Replace DbContext registrations to attach our SaveChanges interceptor (probe)
                services.RemoveAll(typeof(DbContextOptions<GameStoreContext>));

                // OPTION 1
                services.RemoveAll(typeof(GameStoreContext));
                services.RemoveAll(typeof(IDbContextFactory<GameStoreContext>));

                services.AddDbContext<GameStoreContext>((sp, options) =>
                {
                    options.UseNpgsql(dbConnString);
                    // Register probe as a transaction interceptor so it observes commit events
                    options.AddInterceptors(probe);
                });
            }
        );

        await host.StartAsync(CancellationToken);

        // 3) Publish OrderPaid message
        await using (var client = new ServiceBusClient(sbConnString))
        {
            var publisher = new ServiceBusMessagePublisher(client, NullLogger<ServiceBusMessagePublisher>.Instance);

            await publisher.PublishAsync(
                new OrderPaid(orderId),
                queueName: "orders");
        }

        // 4) Wait for the worker to complete the order (probe observes SaveChanges)
        await probe.WaitAsync(TimeSpan.FromSeconds(30));

        // 5) Assert – the order is in Completed state
        await using (var assertCtx = new GameStoreContext(dbOptions))
        {
            var updated = await assertCtx.Orders
                .AsNoTracking()
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == orderId, CancellationToken);

            updated.ShouldNotBeNull();
            updated!.Status.ShouldBe(OrderStatus.Completed);

            // Assert – game codes were assigned for each item with the expected count
            foreach (var item in updated.Items)
            {
                item.GameCodes.ShouldNotBeNull();
                item.GameCodes!.Count.ShouldBe(item.Quantity);
            }
        }

        // Cleanup
        await host.StopAsync(CancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await postgreContainer.DisposeAsync();
        if (serviceBusContainer is not null)
        {
            await serviceBusContainer.DisposeAsync();
        }
        GC.SuppressFinalize(this);
    }
}