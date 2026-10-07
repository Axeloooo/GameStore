using AutoFixture;
using GameStore.Api.Shared.Messaging;
using GameStore.Contracts.Orders;
using GameStore.Data.Models;
using GameStore.IntegrationTests.Data;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Shouldly;
using System.Text.Json;
using Testcontainers.PostgreSql;

namespace GameStore.IntegrationTests.Api;

public class OutboxProcessorTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer postgres = new PostgreSqlBuilder().Build();
    private readonly Fixture fixture = new();
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        await postgres.StartAsync();
        fixture.Customize<DateTimeOffset>(o => o.FromFactory(() => DateTimeOffset.UtcNow));
    }

    [Fact]
    public async Task OutboxProcessor_WithPendingMessage_PublishesAndMarksAsProcessed()
    {
        // Arrange
        var messagePublisherStub = Substitute.For<IMessagePublisher>();

        // 1. Seed a pending outbox message directly in the database
        var orderId = Guid.NewGuid();
        var paymentId = Guid.NewGuid().ToString();
        var orderPaidMessage = new OrderPaid(orderId);

        var outboxMessage = new OutboxMessage
        {
            MessageType = nameof(OrderPaid),
            QueueName = "orders",
            Payload = JsonSerializer.Serialize(orderPaidMessage),
            MessageId = paymentId,
            CorrelationId = orderId.ToString(),
            CreatedAt = DateTime.UtcNow,
            ProcessedAt = null, // Pending - needs processing
            RetryCount = 0
        };

        // Seed the message first to get the database-generated ID
        var setupFactory = new GameStoreWebApplicationFactory(postgres);
        var db = setupFactory.CreateDbContext();
        db.OutboxMessages.Add(outboxMessage);
        await db.SaveChangesAsync(CancellationToken);

        // Now outboxMessage.Id has been set by the database
        var messageId = outboxMessage.Id;

        // Create interceptor to detect when the message is processed
        var probe = new OutboxMessageProcessedInterceptor(messageId);

        // Create a custom WebApplicationFactory with both stubs
        var factoryWithStub = new GameStoreWebApplicationFactory(
            postgres,
            saveChangesInterceptor: probe,
            messagePublisher: messagePublisherStub);

        // 2. Start the API (which starts OutboxProcessor as a hosted service)
        var client = factoryWithStub.CreateClient();

        // 3. Wait for OutboxProcessor to process the message (detected by interceptor)
        await probe.WaitAsync(TimeSpan.FromSeconds(30));

        // 4. Verify the message was published via IMessagePublisher
        await messagePublisherStub.Received(1).PublishAsync(
            nameof(OrderPaid),
            Arg.Is<string>(payload => payload.Contains(orderId.ToString())),
            "orders",
            paymentId,
            orderId.ToString());

        // 5. Verify the message was marked as processed in the database
        var verifyDb = factoryWithStub.CreateDbContext();
        var processedMessage = await verifyDb.OutboxMessages
            .AsNoTracking()
            .FirstAsync(m => m.Id == messageId, CancellationToken);

        processedMessage.ProcessedAt.ShouldNotBeNull(); // Marked as processed
        processedMessage.LastError.ShouldBeNull(); // No errors
        processedMessage.RetryCount.ShouldBe(0); // No retries needed
    }

    public async ValueTask DisposeAsync()
    {
        await postgres.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}
