using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;

namespace GameStore.Api.UnitTests.TestSupport;

// The InMemory provider does not enforce unique indexes, so this interceptor
// reproduces what PostgreSQL does when two requests with the same OperationId
// race: the first SaveChanges fails with a unique violation (SqlState 23505).
// An optional callback runs first, to persist the "winning" competing write.
internal sealed class UniqueViolationInterceptor(Func<Task>? beforeFailing = null)
    : SaveChangesInterceptor
{
    private bool hasFailed;

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (hasFailed)
        {
            return result;
        }

        hasFailed = true;

        if (beforeFailing is not null)
        {
            await beforeFailing();
        }

        throw new DbUpdateException(
            "duplicate key value violates unique constraint",
            new PostgresException(
                "duplicate key value violates unique constraint",
                "ERROR",
                "ERROR",
                PostgresErrorCodes.UniqueViolation));
    }
}
