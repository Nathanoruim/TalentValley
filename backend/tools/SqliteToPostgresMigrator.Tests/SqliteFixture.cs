using Microsoft.Data.Sqlite;
using Npgsql;

namespace SqliteToPostgresMigrator.Tests;

// A SQLite file shaped like the Railway database (everything stored as TEXT/INTEGER, as EF Core's SQLite provider did),
// holding synthetic data only. Its tables mirror the migrated PostgreSQL schema; every column accepts NULL so tests can
// plant the bad values a real database might hold.
internal sealed class SqliteFixture : IDisposable
{
    private readonly string directory = Directory.CreateTempSubdirectory("tv-migrator-").FullName;
    private readonly IReadOnlyList<TargetTable> schema;

    private SqliteFixture(IReadOnlyList<TargetTable> schema)
    {
        this.schema = schema;
        Path = System.IO.Path.Combine(directory, "railway.db");
    }

    public string Path { get; }

    public static async Task<SqliteFixture> CreateAsync(string postgresConnectionString)
    {
        await using var connection = new NpgsqlConnection(postgresConnectionString);
        await connection.OpenAsync();
        var fixture = new SqliteFixture(await PostgresSchema.ReadDataTablesAsync(connection, CancellationToken.None));
        fixture.Execute(connection =>
        {
            Run(connection, "PRAGMA journal_mode = WAL");
            foreach (var table in fixture.schema)
            {
                var columns = string.Join(", ", table.Columns.Select(x => $"{Sql.Quote(x.Name)} {SqliteType(x.DataType)}"));
                Run(connection, $"CREATE TABLE {Sql.Quote(table.Name)} ({columns})");
            }
            // Present in the real database and ignored by the tool.
            Run(connection, "CREATE TABLE \"__EFMigrationsHistory\" (\"MigrationId\" TEXT, \"ProductVersion\" TEXT)");
            Run(connection, "CREATE TABLE \"__EFMigrationsLock\" (\"Id\" INTEGER, \"Timestamp\" TEXT)");
        });
        return fixture;
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        Directory.Delete(directory, recursive: true);
    }

    // Columns left out of `values` get a neutral value when the target requires one, so tests state only what matters.
    public void Insert(string table, params (string Column, object? Value)[] values)
    {
        var definition = schema.Single(x => x.Name == table);
        var row = definition.Columns
            .Select(column => (Column: column, Provided: values.Any(v => v.Column == column.Name),
                Value: values.FirstOrDefault(v => v.Column == column.Name).Value))
            .Where(x => x.Provided || !x.Column.IsNullable)
            .Select(x => (x.Column.Name, Value: x.Provided ? x.Value : DefaultFor(x.Column)))
            .ToList();

        Execute(connection =>
        {
            using var command = connection.CreateCommand();
            command.CommandText = $"INSERT INTO {Sql.Quote(table)} ({string.Join(", ", row.Select(x => Sql.Quote(x.Name)))}) " +
                                  $"VALUES ({string.Join(", ", row.Select((_, i) => $"@p{i}"))})";
            for (var i = 0; i < row.Count; i++)
                command.Parameters.AddWithValue($"p{i}", row[i].Value ?? DBNull.Value);
            command.ExecuteNonQuery();
        });
    }

    public void Execute(string sql) => Execute(connection => Run(connection, sql));

    public static string NewId() => Guid.NewGuid().ToString().ToUpperInvariant();

    private void Execute(Action<SqliteConnection> action)
    {
        using var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = Path,
            Pooling = false
        }.ConnectionString);
        connection.Open();
        action(connection);
    }

    private static void Run(SqliteConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    private static string SqliteType(string dataType) => dataType is "integer" or "boolean" ? "INTEGER" : "TEXT";

    private static object DefaultFor(TargetColumn column) => column.DataType switch
    {
        "uuid" => NewId(),
        "boolean" or "integer" => 0L,
        "timestamp with time zone" => "2026-09-11 12:22:11.1234567+00:00",
        "date" => "2026-01-15",
        _ => "x"
    };
}
