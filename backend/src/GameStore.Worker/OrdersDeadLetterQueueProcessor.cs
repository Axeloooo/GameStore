using Azure.Messaging.ServiceBus;

namespace GameStore.Worker;

public class OrdersDeadLetterQueueProcessor(
    ServiceBusClient serviceBusClient,
    ILogger<OrdersDeadLetterQueueProcessor> logger
) : BackgroundService
{
    private ServiceBusProcessor? dlqProcessor;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        dlqProcessor = serviceBusClient.CreateProcessor(
            "orders",
            new ServiceBusProcessorOptions
            {
                AutoCompleteMessages = false,
                SubQueue = SubQueue.DeadLetter
            });

        dlqProcessor.ProcessMessageAsync += ProcessMessage;
        dlqProcessor.ProcessErrorAsync += ProcessError;

        await dlqProcessor.StartProcessingAsync(stoppingToken);

        logger.LogInformation("Orders DLQ worker started.");

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task ProcessMessage(ProcessMessageEventArgs args)
    {
        try
        {
            var message = args.Message;

            logger.LogError(
                "Order message reached DLQ. MessageId: {MessageId} " +
                "Reason: {DeadLetterReason} " +
                "ErrorDescription: {DeadLetterErrorDescription}" +
                "Subject: {Subject}" +
                "EnqueuedTime: {EnqueuedTime}" +
                "CorrelationId: {CorrelationId}" +
                "DeliveryCount: {DeliveryCount}",
                message.MessageId,
                message.DeadLetterReason,
                message.DeadLetterErrorDescription,
                message.Subject,
                message.EnqueuedTime,
                message.CorrelationId,
                message.DeliveryCount
            );

            await args.CompleteMessageAsync(message, args.CancellationToken);

            logger.LogInformation("Successfully processed DLQ message with MessageId {MessageId}",
                                message.MessageId);
        }
        catch(Exception ex)
        {
            logger.LogError(ex, "Error processing DLQ message with MessageId {MessageId}",
                                args.Message.MessageId);
        }
    }

    private Task ProcessError(ProcessErrorEventArgs args)
    {
        logger.LogError(args.Exception, "Service Bus DLQ processing error: {Error}",
                        args.Exception.Message);

        return Task.CompletedTask;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Orders DLQ worker stopping...");

        if (dlqProcessor is not null)
        {
            await dlqProcessor.StopProcessingAsync(cancellationToken);
            await dlqProcessor.DisposeAsync();
        }

        await base.StopAsync(cancellationToken);
    }
}
