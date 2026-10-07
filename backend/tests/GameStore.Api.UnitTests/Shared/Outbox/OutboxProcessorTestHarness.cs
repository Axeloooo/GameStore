using System.Text.Json;
using GameStore.Api.Shared.Messaging;
using GameStore.Api.Shared.Outbox;
using GameStore.Api.UnitTests.TestSupport;
using GameStore.Contracts.Orders;
using GameStore.Data;
using GameStore.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace GameStore.Api.UnitTests.Shared.Outbox;

// Shared plumbing for the NSubstitute and Moq flavours of the OutboxProcessor
// tests: an in-memory outbox table and a way to run exactly one polling cycle.
internal sealed class OutboxProcessorTestHarness
{
    public static readonly DateTimeOffset Now =
        new(2026, 10, 6, 12, 30, 0, TimeSpan.Zero);

    private readonly InMemoryGameStoreContext database = new();

    public static OutboxMessage NewOrderPaidMessage(
        DateTime createdAt,
        string messageId,
        int retryCount = 0,
        DateTime? processedAt = null)
    {
        var orderId = Guid.NewGuid();

        return new OutboxMessage
        {
            Id = Guid.NewGuid(),
            MessageType = nameof(OrderPaid),
            QueueName = "orders",
            Payload = JsonSerializer.Serialize(new OrderPaid(orderId)),
            MessageId = messageId,
            CorrelationId = orderId.ToString(),
            CreatedAt = createdAt,
            RetryCount = retryCount,
            ProcessedAt = processedAt
        };
    }

    public async Task SeedAsync(params OutboxMessage[] messages)
    {
        using var context = database.Create();
        context.OutboxMessages.AddRange(messages);
        await context.SaveChangesAsync();
    }

    public async Task<OutboxMessage> GetAsync(Guid id)
    {
        using var context = database.Create();
        return await context.OutboxMessages.AsNoTracking().SingleAsync(message => message.Id == id);
    }

    // Starts the hosted service, waits for the first cycle to save its results
    // and stops it again. The 10 second polling delay is cancelled by StopAsync,
    // so a cycle takes milliseconds. Every scenario seeds at least one eligible
    // message, because the processor only saves when it has work to do.
    public async Task RunOneCycleAsync(IMessagePublisher messagePublisher, TimeProvider timeProvider)
    {
        var signal = new SavedChangesSignal();

        await using var services = new ServiceCollection()
            .AddDbContext<GameStoreContext>(options => database.Configure(options, signal))
            .BuildServiceProvider();

        using var sut = new OutboxProcessor(
            services,
            messagePublisher,
            timeProvider,
            NullLogger<OutboxProcessor>.Instance);

        await sut.StartAsync(CancellationToken.None);
        await signal.WaitAsync(TimeSpan.FromSeconds(5));
        await sut.StopAsync(CancellationToken.None);
    }
}
