using System.Text.Json;
using Azure.Messaging.ServiceBus;
using GameStore.Contracts.Orders;
using GameStore.Worker.MessageHandlers;

namespace GameStore.Worker;

public class OrdersQueueProcessor(
    ServiceBusClient serviceBusClient,
    IServiceScopeFactory serviceScopeFactory,
    ILogger<OrdersQueueProcessor> logger) : BackgroundService
{
    private ServiceBusProcessor? processor;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        processor = serviceBusClient.CreateProcessor(
            "orders",
            new ServiceBusProcessorOptions
            {
               AutoCompleteMessages = false
            });

        processor.ProcessMessageAsync += ProcessMessage;
        processor.ProcessErrorAsync += ProcessError;

        await processor.StartProcessingAsync(stoppingToken);
        logger.LogInformation("Started message processing.");

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Order queue worker stopping...");

        if (processor is not null)
        {
            await processor.StopProcessingAsync(cancellationToken);
            await processor.DisposeAsync();
        }

        await base.StopAsync(cancellationToken);
    }

    private async Task ProcessMessage(ProcessMessageEventArgs args)
    {
        try
        {
            var message = args.Message;
            logger.LogInformation("Received message with MessageId {MessageId}",
                                    message.MessageId);

            var messageBody = message.Body.ToString();

            if (!message.ApplicationProperties.TryGetValue("MessageType", out var messageTypeObj)
                || messageTypeObj is not string messageType)
            {
                logger.LogWarning("MessageType not found in application properties. Message will be dead-lettered: {MessageBody}",
                                 messageBody);
                await args.DeadLetterMessageAsync(
                    message,
                    "Invalid message format",
                    "MessageType property missing");
                return;
            }

            logger.LogInformation("Processing message of type {MessageType}...",
                                    messageType);
            await HandleMessageByType(messageBody, messageType, args.CancellationToken);

            await args.CompleteMessageAsync(message);
            logger.LogInformation("Message {MessageId} completed successfully.",
                                    message.MessageId);
        }
        catch(Exception ex)
        {
            logger.LogError(ex, "Error processing message {MessageId}. Message abandoned (delivery count: {DeliveryCount})",
                            args.Message.MessageId,
                            args.Message.DeliveryCount);
            await args.AbandonMessageAsync(args.Message);
        }
    }

    private async Task HandleMessageByType(
        string messageBody,
        string messageType,
        CancellationToken cancellationToken)
    {
        switch (messageType)
        {
            case nameof(OrderPaid):
                await HandleMessageAsync<OrderPaid>(messageBody, cancellationToken);
                break;
            default:
                logger.LogWarning("Unknown message type '{MessageType}' received: {MessageBody}",
                                    messageType,
                                    messageBody);
                break;
        }
    }

    private async Task HandleMessageAsync<T>(string messageBody, CancellationToken cancellationToken)
    {
        try
        {
            var message = JsonSerializer.Deserialize<T>(messageBody);

            if (message is null)
            {
                logger.LogWarning("Deserialized message was null for type {MessageType}",
                                typeof(T).Name);
                return;
            }

            using var scope = serviceScopeFactory.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<IMessageHandler<T>>();
            await handler.HandleAsync(message, cancellationToken);
        }
        catch(JsonException ex)
        {
            logger.LogError(ex, "Error deserializing {MessageType} message: {MessageBody}",
                        typeof(T).Name,
                        messageBody);
        }
    }

    private Task ProcessError(ProcessErrorEventArgs args)
    {
        logger.LogError(args.Exception, "Service Bus processing error: {Error}",
                        args.Exception.Message);
        return Task.CompletedTask;
    }
}
