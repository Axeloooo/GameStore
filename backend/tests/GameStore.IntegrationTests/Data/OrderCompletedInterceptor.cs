using System.Data.Common;
using GameStore.Data;
using GameStore.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace GameStore.IntegrationTests.Data;

public sealed class OrderCompletedInterceptor(Guid targetOrderId) : DbTransactionInterceptor
{
    private readonly TaskCompletionSource taskCompletionSource =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public Task WaitAsync(TimeSpan timeout) =>
        Task.WhenAny(taskCompletionSource.Task, Task.Delay(timeout))
            .ContinueWith(t => taskCompletionSource.Task.IsCompleted
                ? Task.CompletedTask
                : throw new TimeoutException("Order did not reach Completed in time."));

    public override async Task TransactionCommittedAsync(
        DbTransaction transaction,
        TransactionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        // If we've already signaled completion, skip any further checks to avoid redundant work
        if (taskCompletionSource.Task.IsCompleted)
        {
            await base.TransactionCommittedAsync(transaction, eventData, cancellationToken);
            return;
        }

        if (eventData.Context is GameStoreContext ctx)
        {
            // Verify persisted state after commit
            var order = await ctx.Orders.AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == targetOrderId, cancellationToken);

            if (order?.Status == OrderStatus.Completed)
            {
                taskCompletionSource.TrySetResult();
            }
        }

        await base.TransactionCommittedAsync(transaction, eventData, cancellationToken);
    }
}