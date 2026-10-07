using FluentAssertions;
using GameStore.Api.Shared.Messaging;
using Moq;
using static GameStore.Api.UnitTests.Shared.Outbox.OutboxProcessorTestHarness;

namespace GameStore.Api.UnitTests.Shared.Outbox;

// The same subject with Moq instead of NSubstitute, mirroring the course's
// Moq lesson: Setup(...) to stub behaviour, Verify(...) to check interactions.
public class OutboxProcessorMoqTests
{
    private readonly OutboxProcessorTestHarness harness = new();

    [Fact]
    public async Task ExecuteAsync_OneMessageFailsToPublish_StillPublishesAndCompletesTheOthers()
    {
        // Arrange
        var failing = NewOrderPaidMessage(Now.UtcDateTime.AddMinutes(-10), messageId: "failing");
        var healthy = NewOrderPaidMessage(Now.UtcDateTime.AddMinutes(-5), messageId: "healthy");
        await harness.SeedAsync(failing, healthy);

        var timeProviderStub = new Mock<TimeProvider>();
        timeProviderStub.Setup(provider => provider.GetUtcNow()).Returns(Now);

        var messagePublisherMock = new Mock<IMessagePublisher>();
        messagePublisherMock
            .Setup(publisher => publisher.PublishAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), "failing", It.IsAny<string?>()))
            .ThrowsAsync(new TimeoutException("Send timed out"));
        messagePublisherMock
            .Setup(publisher => publisher.PublishAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), "healthy", It.IsAny<string?>()))
            .Returns(Task.CompletedTask);

        // Act
        await harness.RunOneCycleAsync(messagePublisherMock.Object, timeProviderStub.Object);

        // Assert
        messagePublisherMock.Verify(publisher => publisher.PublishAsync(
            It.IsAny<string>(), It.IsAny<string>(), "orders", "failing", failing.CorrelationId), Times.Once);
        messagePublisherMock.Verify(publisher => publisher.PublishAsync(
            It.IsAny<string>(), It.IsAny<string>(), "orders", "healthy", healthy.CorrelationId), Times.Once);

        var failed = await harness.GetAsync(failing.Id);
        failed.RetryCount.Should().Be(1);
        failed.LastError.Should().Be("Send timed out");
        failed.ProcessedAt.Should().BeNull();

        var published = await harness.GetAsync(healthy.Id);
        published.ProcessedAt.Should().Be(Now.UtcDateTime);
        published.RetryCount.Should().Be(0);
    }
}
