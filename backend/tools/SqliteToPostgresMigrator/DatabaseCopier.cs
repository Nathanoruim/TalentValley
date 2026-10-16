using Npgsql;
using NpgsqlTypes;

namespace SqliteToPostgresMigrator;

internal sealed record TableCount(string Table, long Source, long Target);

// Copies every data table of the SQLite database into an empty PostgreSQL database, all in one transaction.
internal static class DatabaseCopier
{
    // Neon Free allows 0.5 GB per project; the SQLite file size is the estimate of what will be stored.
    public const long MaxSqliteBytes = 400L * 1024 * 1024;

    public static async Task<IReadOnlyList<TableCount>> CopyAsync(
        string sqlitePath, string postgresConnectionString, Action<string> log, CancellationToken cancellationToken)
    {
        // Data not yet checkpointed still lives in the -wal file, so it counts toward the estimate.
        var size = new FileInfo(sqlitePath).Length + (File.Exists(sqlitePath + "-wal") ? new FileInfo(sqlitePath + "-wal").Length : 0);
        log($"SQLite de origem: {size / 1024.0 / 1024.0:F1} MB.");
        if (size > MaxSqliteBytes)
            throw new MigrationAbortedException(
                $"O SQLite tem mais de {MaxSqliteBytes / 1024 / 1024} MB; o limite Free do Neon é 0,5 GB. Nada foi copiado.");

        using var source = SqliteSource.Open(sqlitePath);
        await using var target = new NpgsqlConnection(postgresConnectionString);
        await target.OpenAsync(cancellationToken);

        var tables = await PostgresSchema.ReadDataTablesAsync(target, cancellationToken);
        EnsureSchemasMatch(source, tables);
        await EnsureTargetIsEmptyAsync(target, tables, cancellationToken);
        EnsureSourceValuesAreConvertible(source, tables);

        await using var transaction = await target.BeginTransactionAsync(cancellationToken);
        var counts = new List<TableCount>();
        try
        {
            foreach (var table in tables)
            {
                log($"Copiando {table.Name}...");
                await CopyTableAsync(source, target, table, cancellationToken);
                await AlignIdentitySequencesAsync(target, table, cancellationToken);
                counts.Add(new TableCount(table.Name, source.Count(table.Name),
                    await CountAsync(target, table.Name, cancellationToken)));
            }

            var mismatches = counts.Where(x => x.Source != x.Target).Select(x => x.Table).ToList();
            if (mismatches.Count > 0)
                throw new MigrationAbortedException(
                    $"Contagens diferentes entre origem e destino em: {string.Join(", ", mismatches)}. Nada foi gravado.");

            await transaction.CommitAsync(cancellationToken);
        }
        catch (PostgresException exception)
        {
            // The server message and Detail can quote the failing row, which is personal data (S9).
            throw new MigrationAbortedException(
                $"O Postgres rejeitou os dados (SqlState {exception.SqlState}, tabela {exception.TableName}, " +
                $"coluna {exception.ColumnName}, constraint {exception.ConstraintName}). Nada foi gravado.");
        }
        return counts;
    }

    private static void EnsureSchemasMatch(SqliteSource source, IReadOnlyList<TargetTable> tables)
    {
        var problems = new List<string>();
        var sourceTables = source.TableNames();

        foreach (var table in tables)
        {
            if (!sourceTables.Contains(table.Name, StringComparer.Ordinal))
            {
                problems.Add($"tabela {table.Name} não existe na origem");
                continue;
            }

            var sourceColumns = source.ColumnNames(table.Name);
            var targetColumns = table.Columns.Select(x => x.Name).ToList();
            problems.AddRange(targetColumns.Except(sourceColumns, StringComparer.Ordinal)
                .Select(x => $"coluna {table.Name}.{x} não existe na origem"));
            problems.AddRange(sourceColumns.Except(targetColumns, StringComparer.Ordinal)
                .Select(x => $"coluna {table.Name}.{x} não existe no destino"));
            problems.AddRange(table.Columns.Where(x => !ValueConverter.IsSupported(x.DataType))
                .Select(x => $"tipo {x.DataType} de {table.Name}.{x.Name} não é suportado pela ferramenta"));
        }

        problems.AddRange(sourceTables.Except(tables.Select(x => x.Name), StringComparer.Ordinal)
            .Where(x => !PostgresSchema.IsExcluded(x))
            .Select(x => $"tabela {x} existe na origem e não no destino (dados seriam perdidos)"));

        if (problems.Count > 0)
            throw new MigrationAbortedException(
                "Schemas diferentes. Aplique as migrations no destino com 'dotnet ef database update'. " +
                string.Join("; ", problems) + ".");
    }

    private static async Task EnsureTargetIsEmptyAsync(
        NpgsqlConnection target, IReadOnlyList<TargetTable> tables, CancellationToken cancellationToken)
    {
        var filled = new List<string>();
        foreach (var table in tables)
        {
            await using var command = new NpgsqlCommand($"SELECT EXISTS (SELECT 1 FROM {Sql.Quote(table.Name)})", target);
            if ((bool)(await command.ExecuteScalarAsync(cancellationToken))!) filled.Add(table.Name);
        }
        if (filled.Count > 0)
            throw new MigrationAbortedException(
                $"O destino não está vazio (tabelas com dados: {string.Join(", ", filled)}). Nada foi copiado.");
    }

    // A first pass over the source finds every value the target would reject, so the copy never starts half-way
    // and the report lists table, column and count only.
    private static void EnsureSourceValuesAreConvertible(SqliteSource source, IReadOnlyList<TargetTable> tables)
    {
        var problems = new List<string>();
        foreach (var table in tables)
        {
            var counts = new Dictionary<(string Column, ValueProblem Problem), long>();
            using var reader = source.Select(table.Name, table.Columns.Select(x => x.Name));
            while (reader.Read())
            {
                for (var i = 0; i < table.Columns.Count; i++)
                {
                    var problem = ValueConverter.TryConvert(reader.GetValue(i), table.Columns[i], out _);
                    if (problem == ValueProblem.None) continue;
                    var key = (table.Columns[i].Name, problem);
                    counts[key] = counts.GetValueOrDefault(key) + 1;
                }
            }
            problems.AddRange(counts.Select(x => $"{table.Name}.{x.Key.Column}: {x.Value} valor(es) {Describe(x.Key.Problem)}"));
        }

        if (problems.Count > 0)
            throw new MigrationAbortedException(
                $"Valores que o destino não aceita: {string.Join("; ", problems)}. Nada foi copiado.");
    }

    private static string Describe(ValueProblem problem) => problem switch
    {
        ValueProblem.NullInRequiredColumn => "nulo(s) em coluna obrigatória",
        ValueProblem.ExceedsMaxLength => "acima do tamanho máximo da coluna",
        _ => "em formato inválido"
    };

    private static async Task CopyTableAsync(
        SqliteSource source, NpgsqlConnection target, TargetTable table, CancellationToken cancellationToken)
    {
        var columnList = string.Join(", ", table.Columns.Select(x => Sql.Quote(x.Name)));
        await using var writer = await target.BeginBinaryImportAsync(
            $"COPY {Sql.Quote(table.Name)} ({columnList}) FROM STDIN (FORMAT BINARY)", cancellationToken);
        using var reader = source.Select(table.Name, table.Columns.Select(x => x.Name));

        while (reader.Read())
        {
            await writer.StartRowAsync(cancellationToken);
            for (var i = 0; i < table.Columns.Count; i++)
            {
                var column = table.Columns[i];
                var problem = ValueConverter.TryConvert(reader.GetValue(i), column, out var value);
                if (problem != ValueProblem.None)
                    throw new MigrationAbortedException($"Valor inesperado em {table.Name}.{column.Name}. Nada foi gravado.");
                await WriteAsync(writer, value, column, cancellationToken);
            }
        }
        await writer.CompleteAsync(cancellationToken);
    }

    private static Task WriteAsync(NpgsqlBinaryImporter writer, object? value, TargetColumn column,
        CancellationToken cancellationToken) => value switch
    {
        null => writer.WriteNullAsync(cancellationToken),
        Guid guid => writer.WriteAsync(guid, NpgsqlDbType.Uuid, cancellationToken),
        bool flag => writer.WriteAsync(flag, NpgsqlDbType.Boolean, cancellationToken),
        int number => writer.WriteAsync(number, NpgsqlDbType.Integer, cancellationToken),
        DateTime stamp => writer.WriteAsync(stamp, NpgsqlDbType.TimestampTz, cancellationToken),
        DateOnly day => writer.WriteAsync(day, NpgsqlDbType.Date, cancellationToken),
        string text => writer.WriteAsync(text,
            column.DataType == "text" ? NpgsqlDbType.Text : NpgsqlDbType.Varchar, cancellationToken),
        _ => throw new MigrationAbortedException($"Tipo inesperado para {column.Name}.")
    };

    // Rows were inserted with their original ids, so the sequences must continue after the highest one (catalogs and claims).
    private static async Task AlignIdentitySequencesAsync(
        NpgsqlConnection target, TargetTable table, CancellationToken cancellationToken)
    {
        foreach (var column in table.Columns.Where(x => x.IsIdentity))
        {
            var quotedColumn = Sql.Quote(column.Name);
            var sql = $"SELECT setval(pg_get_serial_sequence(@table, @column), COALESCE(MAX({quotedColumn}), 1), " +
                      $"MAX({quotedColumn}) IS NOT NULL) FROM {Sql.Quote(table.Name)}";
            await using var command = new NpgsqlCommand(sql, target);
            command.Parameters.AddWithValue("table", Sql.Quote(table.Name));
            command.Parameters.AddWithValue("column", column.Name);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private static async Task<long> CountAsync(NpgsqlConnection target, string table, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand($"SELECT COUNT(*) FROM {Sql.Quote(table)}", target);
        return (long)(await command.ExecuteScalarAsync(cancellationToken))!;
    }
}
