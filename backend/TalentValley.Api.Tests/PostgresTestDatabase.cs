using Microsoft.EntityFrameworkCore;
using Npgsql;
using TalentValley.Api.Data;

namespace TalentValley.Api.Tests;

/// <summary>
/// Gives every test factory its own PostgreSQL database. A template database with the migrations applied is
/// created once per test run; each factory clones it (fast) and drops its copy on dispose.
/// </summary>
public static class PostgresTestDatabase
{
    public const string EnvironmentVariable = "TALENT_VALLEY_TEST_POSTGRES";
    private const string NamePrefix = "tv_test_";
    private const string TemplatePrefix = "tv_template_";

    private static readonly Lazy<string> Template = new(CreateTemplate, LazyThreadSafetyMode.ExecutionAndPublication);

    private static string ServerConnectionString()
    {
        var value = Environment.GetEnvironmentVariable(EnvironmentVariable);
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException(
                $"Defina a variável de ambiente {EnvironmentVariable} com a connection string de um servidor PostgreSQL " +
                "que permita CREATE DATABASE. Exemplo: docker run --name tv-pg -e POSTGRES_PASSWORD=<senha> -p 5432:5432 -d postgres:17 " +
                $"e {EnvironmentVariable}=\"Host=localhost;Username=postgres;Password=<senha>\".");
        return new NpgsqlConnectionStringBuilder(value) { Database = "postgres", Pooling = false }.ConnectionString;
    }

    public static string CreateDatabase()
    {
        var name = NamePrefix + Guid.NewGuid().ToString("N");
        Execute($"CREATE DATABASE \"{name}\" TEMPLATE \"{Template.Value}\"");
        return ConnectionStringFor(name);
    }

    public static void DropDatabase(string connectionString)
    {
        var name = new NpgsqlConnectionStringBuilder(connectionString).Database!;
        NpgsqlConnection.ClearAllPools();
        Execute($"DROP DATABASE IF EXISTS \"{name}\" WITH (FORCE)");
    }

    public static DbContextOptions<AppDbContext> Options(string connectionString) =>
        new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(connectionString).Options;

    private static string ConnectionStringFor(string database) =>
        new NpgsqlConnectionStringBuilder(ServerConnectionString())
        {
            Database = database,
            Pooling = true,
            MaxPoolSize = 5
        }.ConnectionString;

    private static string CreateTemplate()
    {
        var name = TemplatePrefix + Guid.NewGuid().ToString("N");
        Execute($"CREATE DATABASE \"{name}\"");
        var connectionString = new NpgsqlConnectionStringBuilder(ServerConnectionString())
        {
            Database = name,
            Pooling = false
        }.ConnectionString;
        using (var db = new AppDbContext(Options(connectionString)))
            db.Database.Migrate(); // Explicit test setup, never application startup.
        // A template database cannot be cloned while connections to it remain open.
        NpgsqlConnection.ClearAllPools();
        // Interrupted runs can leave tv_template_*/tv_test_* databases behind; they are safe to drop manually.
        AppDomain.CurrentDomain.ProcessExit += (_, _) => Execute($"DROP DATABASE IF EXISTS \"{name}\" WITH (FORCE)");
        return name;
    }

    private static void Execute(string sql)
    {
        using var connection = new NpgsqlConnection(ServerConnectionString());
        connection.Open();
        using var command = new NpgsqlCommand(sql, connection);
        command.ExecuteNonQuery();
    }
}
