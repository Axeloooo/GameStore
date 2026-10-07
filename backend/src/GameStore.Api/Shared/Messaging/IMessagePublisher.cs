namespace GameStore.Api.Shared.Messaging;

public interface IMessagePublisher
{
    Task PublishAsync<T>(
        T message,
        string queueName,
        string? messageId = null,
        string? correlationId = null);

    Task PublishAsync(
        string messageType,
        string messageBody,
        string queueName,
        string? messageId = null,
        string? correlationId = null
    );
}
