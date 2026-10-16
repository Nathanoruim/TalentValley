using Npgsql;

namespace SqliteToPostgresMigrator;

internal sealed record TargetColumn(string Name, string DataType, int? MaxLength, bool IsNullable, bool IsIdentity);

internal sealed record TargetTable(string Name, IReadOnlyList<TargetColumn> Columns);

// Reads the target schema from the catalogs, so the copy follows whatever the EF migrations created.
internal static class PostgresSchema
{
    // Bookkeeping that is not data to copy. Old DataProtection keys are deliberately left behind (S13).
    private static readonly string[] ExcludedTables = ["__EFMigrationsHistory", "DataProtectionKeys"];

    public static bool IsExcluded(string table) => ExcludedTables.Contains(table, StringComparer.Ordinal);

    // Returns the data tables ordered so that every table comes after the tables its foreign keys point to.
    public static async Task<IReadOnlyList<TargetTable>> ReadDataTablesAsync(
        NpgsqlConnection connection, CancellationToken cancellationToken)
    {
        var columns = await ReadColumnsAsync(connection, cancellationToken);
        var dependencies = await ReadDependenciesAsync(connection, cancellationToken);
        return OrderByDependency(columns, dependencies);
    }

    private static async Task<Dictionary<string, List<TargetColumn>>> ReadColumnsAsync(
        NpgsqlConnection connection, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT c.table_name, c.column_name, c.data_type, c.character_maximum_length, c.is_nullable, c.is_identity
            FROM information_schema.columns c
            JOIN information_schema.tables t
              ON t.table_schema = c.table_schema AND t.table_name = c.table_name
            WHERE c.table_schema = 'public' AND t.table_type = 'BASE TABLE'
            ORDER BY c.table_name, c.ordinal_position
            """;
        var tables = new Dictionary<string, List<TargetColumn>>(StringComparer.Ordinal);
        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var table = reader.GetString(0);
            if (IsExcluded(table)) continue;
            if (!tables.TryGetValue(table, out var list)) tables[table] = list = [];
            list.Add(new TargetColumn(
                reader.GetString(1),
                reader.GetString(2),
                reader.IsDBNull(3) ? null : reader.GetInt32(3),
                reader.GetString(4) == "YES",
                reader.GetString(5) == "YES"));
        }
        return tables;
    }

    private static async Task<List<(string Table, string Parent)>> ReadDependenciesAsync(
        NpgsqlConnection connection, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT child.relname, parent.relname
            FROM pg_constraint con
            JOIN pg_class child ON child.oid = con.conrelid
            JOIN pg_class parent ON parent.oid = con.confrelid
            JOIN pg_namespace ns ON ns.oid = child.relnamespace
            WHERE con.contype = 'f' AND ns.nspname = 'public'
            """;
        var result = new List<(string, string)>();
        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            result.Add((reader.GetString(0), reader.GetString(1)));
        return result;
    }

    private static List<TargetTable> OrderByDependency(
        Dictionary<string, List<TargetColumn>> columns, List<(string Table, string Parent)> dependencies)
    {
        var pending = columns.Keys.ToDictionary(
            name => name,
            name => dependencies.Where(d => d.Table == name && d.Parent != name && columns.ContainsKey(d.Parent))
                .Select(d => d.Parent).ToHashSet(StringComparer.Ordinal),
            StringComparer.Ordinal);
        var ordered = new List<TargetTable>();

        while (pending.Count > 0)
        {
            var ready = pending.Where(x => x.Value.Count == 0).Select(x => x.Key)
                .OrderBy(x => x, StringComparer.Ordinal).ToList();
            if (ready.Count == 0)
                throw new MigrationAbortedException(
                    "Dependência circular entre tabelas do destino; a ordem de cópia não pôde ser definida.");
            foreach (var name in ready)
            {
                pending.Remove(name);
                ordered.Add(new TargetTable(name, columns[name]));
            }
            foreach (var remaining in pending.Values) remaining.ExceptWith(ready);
        }
        return ordered;
    }
}
