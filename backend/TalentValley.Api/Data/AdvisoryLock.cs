using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace TalentValley.Api.Data;

public static class AdvisoryLock
{
    /// <summary>
    /// Takes a PostgreSQL advisory lock that is released automatically when the current transaction ends.
    /// Concurrent transactions asking for the same <paramref name="key"/> run one after the other, which
    /// PostgreSQL's default READ COMMITTED level does not guarantee for read-then-write sections.
    /// Must be called after the transaction was opened.
    /// </summary>
    public static async Task AcquireTransactionLockAsync(
        this DatabaseFacade database, string key, CancellationToken cancellationToken = default)
    {
        if (database.CurrentTransaction is null)
            throw new InvalidOperationException("O lock de transação exige uma transação aberta.");
        await database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({key}, 0))", cancellationToken);
    }
}
