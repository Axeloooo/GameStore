using FluentAssertions;
using GameStore.Api.Shared.Messaging;
using GameStore.Contracts.Orders;
using NSubstitute;
using static GameStore.Api.UnitTests.Shared.Outbox.OutboxProcessorTestHarness;

namespace GameStore.Api.UnitTests.Shared.Outbox;

public class OutboxProcessorTests
{
    private readonly OutboxProcessorTestHarness harness = new();

    // Mock: the test verifies the calls the processor makes on it.
    private readonly IMessagePublisher messagePublisherMock = Substitute.For<IMessagePublisher>();

    // Stub: only provides the current time.
    private readonly TimeProvider timeProviderStub = Substitute.For<TimeProvider>();

    public OutboxProcessorTests()
    {
        timeProviderStub.GetUtcNow().Returns(Now);
    }

    [Fact]
    public async Task ExecuteAsync_PendingMessage_PublishesItWithStoredMetadata()
    {
        // Arrange
        var message = NewOrderPaidMessage(Now.UtcDateTime.AddMinutes(-1), messageId: "pi_123");
        await harness.SeedAsync(message);

        // Act
        await harness.RunOneCycleAsync(messagePublisherMock, timeProviderStub);

        // Assert
        await messagePublisherMock.Received(1).PublishAsync(
            nameof(OrderPaid),
            message.Payload,
            "orders",
            "pi_123",
            message.CorrelationId);
    }

    [Fact]
    public async Task ExecuteAsync_PendingMessage_MarksItProcessedAtCurrentUtcTime()
    {
        // Arrange
        var createdAt = Now.UtcDateTime.AddMinutes(-2);
        var message = NewOrderPaidMessage(createdAt, messageId: "pi_123");
        await harness.SeedAsync(message);

        // Act
        await harness.RunOneCycleAsync(messagePublisherMock, timeProviderStub);

        // Assert
        var processed = await harness.GetAsync(message.Id);
        processed.ProcessedAt.Should().NotBeNull();
        processed.ProcessedAt.Should().Be(new DateTime(2026, 10, 6, 12, 30, 0, DateTimeKind.Utc));
        processed.ProcessedAt!.Value.Kind.Should().Be(DateTimeKind.Utc);
        processed.ProcessedAt.Should().BeAfter(createdAt);
        processed.ProcessedAt.Should().BeWithin(TimeSpan.FromMinutes(2)).After(createdAt);
        processed.ProcessedAt.Should().BeCloseTo(Now.UtcDateTime, TimeSpan.FromMilliseconds(1));
        processed.LastError.Should().BeNull();
        processed.RetryCount.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteAsync_SeveralPendingMessages_PublishesOldestFirst()
    {
        // Arrange (seeded newest first on purpose)
        var newest = NewOrderPaidMessage(Now.UtcDateTime.AddMinutes(-1), messageId: "newest");
        var middle = NewOrderPaidMessage(Now.UtcDateTime.AddMinutes(-5), messageId: "middle");
        var oldest = NewOrderPaidMessage(Now.UtcDateTime.AddMinutes(-10), messageId: "oldest");
        await harness.SeedAsync(newest, middle, oldest);

        // Act
        await harness.RunOneCycleAsync(messagePublisherMock, timeProviderStub);

        // Assert
        Received.InOrder(() =>
        {
            messagePublisherMock.PublishAsync(nameof(OrderPaid), oldest.Payload, "orders", "oldest", oldest.CorrelationId);
            messagePublisherMock.PublishAsync(nameof(OrderPaid), middle.Payload, "orders", "middle", middle.CorrelationId);
            messagePublisherMock.PublishAsync(nameof(OrderPaid), newest.Payload, "orders", "newest", newest.CorrelationId);
        });
    }

    [Fact]
    public async Task ExecuteAsync_ProcessedOrRetryExhaustedMessages_AreNotPublishedAgain()
    {
        // Arrange
        var alreadyProcessed = NewOrderPaidMessage(
            Now.UtcDateTime.AddHours(-1), messageId: "processed", processedAt: Now.UtcDateTime.AddMinutes(-59));
        var exhausted = NewOrderPaidMessage(Now.UtcDateTime.AddHours(-1), messageId: "exhausted", retryCount: 3);
        var lastAttempt = NewOrderPaidMessage(Now.UtcDateTime.AddHours(-1), messageId: "last-attempt", retryCount: 2);
        await harness.SeedAsync(alreadyProcessed, exhausted, lastAttempt);

        // Act
        await harness.RunOneCycleAsync(messagePublisherMock, timeProviderStub);

        // Assert
        await messagePublisherMock.DidNotReceive().PublishAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), "processed", Arg.Any<string?>());
        await messagePublisherMock.DidNotReceive().PublishAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), "exhausted", Arg.Any<string?>());
        await messagePublisherMock.Received(1).PublishAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), "last-attempt", Arg.Any<string?>());

        (await harness.GetAsync(alreadyProcessed.Id)).ProcessedAt
            .Should().Be(Now.UtcDateTime.AddMinutes(-59), "an already processed message keeps its original timestamp");
    }

    [Fact]
    public async Task ExecuteAsync_PublisherThrows_IncrementsRetryCountAndRecordsError()
    {
        // Arrange
        var message = NewOrderPaidMessage(Now.UtcDateTime.AddMinutes(-1), messageId: "pi_123", retryCount: 1);
        await harness.SeedAsync(message);

        messagePublisherMock
            .PublishAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>())
            .Returns(Task.FromException(new InvalidOperationException("Service Bus unavailable")));

        // Act
        await harness.RunOneCycleAsync(messagePublisherMock, timeProviderStub);

        // Assert
        var failed = await harness.GetAsync(message.Id);
        failed.RetryCount.Should().Be(2);
        failed.LastError.Should().Be("Service Bus unavailable");
        failed.ProcessedAt.Should().BeNull();
    }
}
