using Microsoft.EntityFrameworkCore.Diagnostics;

namespace GameStore.Api.UnitTests.TestSupport;

// Completes when a DbContext finishes a SaveChanges call. Background services
// such as OutboxProcessor save once per polling cycle, so a test can await this
// signal instead of sleeping for an arbitrary amount of time.
internal sealed class SavedChangesSignal : SaveChangesInterceptor
{
    private readonly TaskCompletionSource saved =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public Task WaitAsync(TimeSpan timeout) => saved.Task.WaitAsync(timeout);

    public override ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        saved.TrySetResult();
        return ValueTask.FromResult(result);
    }
}
