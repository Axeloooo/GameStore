
using GameStore.Data;
using GameStore.Api.Shared.Authorization;
using GameStore.Api.Shared.Messaging;
using Microsoft.EntityFrameworkCore;

namespace GameStore.Api.Shared.Outbox;

public class OutboxProcessor(
    IServiceProvider serviceProvider,
    ServiceBusMessagePublisher messagePublisher,
    TimeProvider timeProvider,
    ILogger<OutboxProcessor> logger
) : BackgroundService
{
    private readonly TimeSpan pollingInterval = TimeSpan.FromSeconds(10);
    private readonly int maxRetryCount = 3;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Outbox Processor started. Polling every {Interval} seconds.",
                             pollingInterval.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxMessagesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing outbox messages.");
            }

            await Task.Delay(pollingInterval, stoppingToken);
        }

        logger.LogInformation("Outbox Processor stopped.");
    }

    private async Task ProcessOutboxMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<GameStoreContext>();

        var messages = await context.OutboxMessages
                        .Where(message => message.ProcessedAt == null
                                && message.RetryCount < maxRetryCount)
                        .OrderBy(message => message.CreatedAt)
                        .ToListAsync(cancellationToken);

        if (messages.Count == 0)
        {
            return;
        }

        logger.LogInformation("Processing {Count} outbox messages", messages.Count);

        foreach (var message in messages)
        {
            try
            {
                await messagePublisher.PublishAsync(
                    message.MessageType,
                    message.Payload,
                    message.QueueName,
                    message.MessageId,
                    message.CorrelationId
                );

                message.ProcessedAt = timeProvider.GetUtcNow().UtcDateTime;
                message.LastError = null;

                logger.LogInformation("Successfully published outbox message {MessageId} of type {MessageType}",
                                        message.MessageId,
                                        message.MessageType);
            }
            catch (Exception ex)
            {
                message.RetryCount++;
                message.LastError = ex.Message;

                logger.LogError(ex, "Failed to publish outbox message {MessageId}. Retry count {RetryCount}",
                                message.MessageId,
                                message.RetryCount);

                if (message.RetryCount >= maxRetryCount)
                {
                    logger.LogCritical("Outbox message {MessageId} exceeded max retry count. Manual intervention required!",
                                        message.MessageId);
                }
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
