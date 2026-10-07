using System.Text.Json;
using Azure.Messaging.ServiceBus;
using FluentAssertions;
using GameStore.Api.Shared.Messaging;
using GameStore.Contracts.Orders;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace GameStore.Api.UnitTests.Shared.Messaging;

// The Azure SDK clients expose virtual members and protected constructors for
// mocking, so no Service Bus namespace or emulator is needed here.
//
// The raw-string overload is called with all five arguments: with only three,
// PublishAsync(string, string, string) and PublishAsync<T>(T, string, ...) are
// ambiguous for the compiler.
public class ServiceBusMessagePublisherTests
{
    private const string QueueName = "orders";

    private readonly ServiceBusClient serviceBusClientStub = Substitute.For<ServiceBusClient>();
    private readonly ServiceBusSender senderMock = Substitute.For<ServiceBusSender>();
    private readonly ServiceBusMessagePublisher sut;
    private ServiceBusMessage? sentMessage;

    public ServiceBusMessagePublisherTests()
    {
        serviceBusClientStub.CreateSender(QueueName).Returns(senderMock);
        senderMock
            .SendMessageAsync(Arg.Do<ServiceBusMessage>(message => sentMessage = message), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        sut = new ServiceBusMessagePublisher(
            serviceBusClientStub,
            NullLogger<ServiceBusMessagePublisher>.Instance);
    }

    [Fact]
    public async Task PublishAsync_TypedMessage_SendsJsonBodyWithTypeNameAndIds()
    {
        // Arrange
        var orderId = Guid.Parse("3f1d2a5e-1111-4c3b-9a0e-6b1f2c3d4e5f");

        // Act
        await sut.PublishAsync(new OrderPaid(orderId), QueueName, "pi_123", orderId.ToString());

        // Assert
        await senderMock.Received(1).SendMessageAsync(Arg.Any<ServiceBusMessage>(), Arg.Any<CancellationToken>());

        sentMessage.Should().NotBeNull();
        sentMessage!.Body.ToString().Should().Be("""{"OrderId":"3f1d2a5e-1111-4c3b-9a0e-6b1f2c3d4e5f"}""");
        JsonSerializer.Deserialize<OrderPaid>(sentMessage.Body.ToString())
            .Should().Be(new OrderPaid(orderId));
        sentMessage.Subject.Should().Be(nameof(OrderPaid));
        sentMessage.ContentType.Should().Be("application/json");
        sentMessage.MessageId.Should().Be("pi_123");
        sentMessage.CorrelationId.Should().Be(orderId.ToString());
        sentMessage.ApplicationProperties.Should()
            .ContainKey("MessageType")
            .WhoseValue.Should().Be(nameof(OrderPaid));
    }

    [Fact]
    public async Task PublishAsync_NoMessageId_GeneratesAGuidMessageId()
    {
        // Act
        await sut.PublishAsync("OrderPaid", "{}", QueueName, messageId: null, correlationId: null);

        // Assert
        sentMessage!.MessageId.Should().NotBeNullOrWhiteSpace();
        Guid.TryParse(sentMessage.MessageId, out _).Should().BeTrue();
        sentMessage.CorrelationId.Should().BeNull();
    }

    [Fact]
    public async Task PublishAsync_AnyMessage_SendsToTheRequestedQueueAndDisposesTheSender()
    {
        // Act
        await sut.PublishAsync("OrderPaid", "{}", QueueName, messageId: null, correlationId: null);

        // Assert
        serviceBusClientStub.Received(1).CreateSender(QueueName);
        await senderMock.Received(1).DisposeAsync();
    }

    [Fact]
    public async Task PublishAsync_SendFails_RethrowsTheServiceBusException()
    {
        // Arrange
        senderMock
            .SendMessageAsync(Arg.Any<ServiceBusMessage>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new ServiceBusException("Queue not found", ServiceBusFailureReason.MessagingEntityNotFound));

        // Act
        var act = () => sut.PublishAsync("OrderPaid", "{}", QueueName, messageId: null, correlationId: null);

        // Assert
        (await act.Should().ThrowAsync<ServiceBusException>())
            .Which.Reason.Should().Be(ServiceBusFailureReason.MessagingEntityNotFound);
        await senderMock.Received(1).DisposeAsync();
    }
}
