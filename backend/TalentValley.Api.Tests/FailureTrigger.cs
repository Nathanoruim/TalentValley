using Microsoft.EntityFrameworkCore;
using TalentValley.Api.Data;

namespace TalentValley.Api.Tests;

/// <summary>
/// Injects a database failure so tests can prove that a request rolls back everything it wrote.
/// Each test database is its own clone, so the function and trigger never leak between tests.
/// </summary>
public static class FailureTrigger
{
    private const string FunctionSql = """
        CREATE OR REPLACE FUNCTION tv_test_fail() RETURNS trigger LANGUAGE plpgsql AS $$
        BEGIN
            RAISE EXCEPTION 'test failure';
        END;
        $$
        """;

    /// <summary>
    /// Makes <paramref name="events"/> on <paramref name="table"/> fail.
    /// <paramref name="table"/>, <paramref name="events"/> and <paramref name="when"/> are fixed strings written
    /// in the tests (never user input); identifiers must already be double-quoted, for example
    /// <c>"Auditorias"</c>, <c>INSERT</c> or <c>UPDATE OF "Ordem"</c>.
    /// </summary>
    public static async Task CreateAsync(AppDbContext db, string name, string table, string events, string? when = null)
    {
        await db.Database.ExecuteSqlRawAsync(FunctionSql);
        var condition = when is null ? "" : $" WHEN ({when})";
#pragma warning disable EF1002 // Identifiers are fixed strings from the tests; SQL cannot parameterize them.
        await db.Database.ExecuteSqlRawAsync(
            $"CREATE TRIGGER {name} BEFORE {events} ON {table} FOR EACH ROW{condition} EXECUTE FUNCTION tv_test_fail()");
#pragma warning restore EF1002
    }
}
