using GameStore.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace GameStore.IntegrationTests.Data;

public sealed class OutboxMessageProcessedInterceptor(Guid targetMessageId) : SaveChangesInterceptor
{
    private readonly TaskCompletionSource taskCompletionSource =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public Task WaitAsync(TimeSpan timeout) =>
        Task.WhenAny(taskCompletionSource.Task, Task.Delay(timeout))
            .ContinueWith(t => taskCompletionSource.Task.IsCompleted
                ? Task.CompletedTask
                : throw new TimeoutException("Outbox message was not marked as processed in time."));

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        // If we've already signaled completion, skip any further checks
        if (!taskCompletionSource.Task.IsCompleted && eventData.Context is GameStoreContext ctx)
        {
            // Check if our target message was processed (ProcessedAt was set)
            var message = await ctx.OutboxMessages.AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == targetMessageId, cancellationToken);

            if (message?.ProcessedAt != null)
            {
                taskCompletionSource.TrySetResult();
            }
        }

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }
}
