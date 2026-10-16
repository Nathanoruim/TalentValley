using Microsoft.Data.Sqlite;

namespace SqliteToPostgresMigrator;

// Read-only view of the Railway database copy.
internal sealed class SqliteSource : IDisposable
{
    private readonly SqliteConnection connection;

    private SqliteSource(SqliteConnection connection) => this.connection = connection;

    public static SqliteSource Open(string path)
    {
        if (!File.Exists(path))
            throw new MigrationAbortedException("Arquivo SQLite não encontrado.");
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = path,
            Mode = SqliteOpenMode.ReadOnly,
            Pooling = false
        }.ConnectionString);
        connection.Open();
        return new SqliteSource(connection);
    }

    public void Dispose() => connection.Dispose();

    // User tables only; SQLite internals and the EF history/lock tables are not data.
    public IReadOnlyList<string> TableNames()
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%' ORDER BY name";
        return Read(command).Where(name => !name.StartsWith("__EFMigrations", StringComparison.Ordinal)).ToList();
    }

    public IReadOnlyList<string> ColumnNames(string table)
    {
        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT name FROM pragma_table_info({Literal(table)}) ORDER BY cid";
        return Read(command);
    }

    public long Count(string table)
    {
        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT COUNT(*) FROM {Sql.Quote(table)}";
        return (long)command.ExecuteScalar()!;
    }

    public SqliteDataReader Select(string table, IEnumerable<string> columns)
    {
        // The command is not disposed here: disposing it would also dispose the reader returned to the caller.
        var command = connection.CreateCommand();
        command.CommandText = $"SELECT {string.Join(", ", columns.Select(Sql.Quote))} FROM {Sql.Quote(table)}";
        return command.ExecuteReader();
    }

    public IReadOnlyList<string> DistinctValues(string table, string column)
    {
        using var command = connection.CreateCommand();
        var quoted = Sql.Quote(column);
        command.CommandText = $"SELECT DISTINCT {quoted} FROM {Sql.Quote(table)} WHERE {quoted} IS NOT NULL AND {quoted} <> ''";
        return Read(command);
    }

    private static List<string> Read(SqliteCommand command)
    {
        var values = new List<string>();
        using var reader = command.ExecuteReader();
        while (reader.Read()) values.Add(reader.GetString(0));
        return values;
    }

    // pragma_table_info takes a string literal, not an identifier.
    private static string Literal(string value) => $"'{value.Replace("'", "''")}'";
}
