using Npgsql;
using TalentValley.Api.Tests;

namespace SqliteToPostgresMigrator.Tests;

public sealed class DatabaseCopierTests : IAsyncLifetime
{
    private const string PersonalMarker = "SEGREDO-PII";
    private static readonly string[] SeededTables =
    [
        "AspNetRoles", "AspNetUsers", "AspNetUserRoles", "AspNetUserClaims", "Alunos", "Competencias", "Idiomas",
        "AlunoCompetencias", "Formacoes", "Auditorias"
    ];

    private string target = string.Empty;
    private SqliteFixture source = null!;
    private readonly string userId = SqliteFixture.NewId();

    public async Task InitializeAsync()
    {
        target = PostgresTestDatabase.CreateDatabase();
        source = await SqliteFixture.CreateAsync(target);
    }

    public Task DisposeAsync()
    {
        source.Dispose();
        PostgresTestDatabase.DropDatabase(target);
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Copies_every_table_converting_types_and_keeping_catalog_ids()
    {
        SeedRealisticData();

        var counts = await Copy();

        Assert.All(counts, x => Assert.Equal(x.Source, x.Target));
        Assert.Equal(SeededTables.Length, counts.Count(x => x.Source > 0));
        Assert.Equal(userId, (await Scalar<Guid>("""SELECT "Id" FROM "AspNetUsers" """)).ToString().ToUpperInvariant());
        Assert.True(await Scalar<bool>("""SELECT "EmailConfirmed" FROM "AspNetUsers" """));
        Assert.False(await Scalar<bool>("""SELECT "TwoFactorEnabled" FROM "AspNetUsers" """));
        Assert.Equal(2, await Scalar<int>("""SELECT "AccessFailedCount" FROM "AspNetUsers" """));
        Assert.Equal("hash-sintetico", await Scalar<string>("""SELECT "PasswordHash" FROM "AspNetUsers" """));
        // A -03:00 offset is normalized to UTC; a value without offset (old Auditoria column) was already UTC.
        Assert.Equal(new DateTime(2026, 9, 11, 15, 22, 11, 123, DateTimeKind.Utc).AddTicks(4560),
            await Scalar<DateTime>("""SELECT "CriadoEm" FROM "AspNetUsers" """));
        Assert.Equal(new DateTime(2026, 9, 11, 12, 22, 11, DateTimeKind.Utc),
            await Scalar<DateTime>("""SELECT "CriadoEm" FROM "Auditorias" """));
        Assert.Equal(new DateOnly(2024, 2, 1), await Scalar<DateOnly>("""SELECT "DataInicio" FROM "Formacoes" """));
        Assert.Equal(new DateOnly(2025, 12, 15), await Scalar<DateOnly>("""SELECT "DataFim" FROM "Formacoes" """));
        Assert.True(await Scalar<bool>("""SELECT "CargaHoraria" IS NULL FROM "Formacoes" """));
        Assert.Equal([7, 12, 40], await List<int>("""SELECT "Id" FROM "Competencias" ORDER BY "Id" """));
        Assert.Equal(12, await Scalar<int>("""SELECT "CompetenciaId" FROM "AlunoCompetencias" """));
    }

    [Fact]
    public async Task Identity_sequences_continue_after_the_highest_copied_id()
    {
        SeedRealisticData();

        await Copy();

        Assert.Equal(41, await NextValue("Competencias", "Id"));
        Assert.Equal(10, await NextValue("Idiomas", "Id"));
        Assert.Equal(6, await NextValue("AspNetUserClaims", "Id"));
        // A table copied with no rows still starts at 1.
        Assert.Equal(1, await NextValue("AspNetRoleClaims", "Id"));
    }

    [Fact]
    public async Task Refuses_a_target_that_already_has_data_and_copies_nothing()
    {
        SeedRealisticData();
        await Execute("""INSERT INTO "AspNetRoles" ("Id", "Name") VALUES (gen_random_uuid(), 'EXISTENTE')""");

        var exception = await Assert.ThrowsAsync<MigrationAbortedException>(Copy);

        Assert.Contains("AspNetRoles", exception.Message);
        Assert.Equal(0, await Scalar<long>("""SELECT COUNT(*) FROM "AspNetUsers" """));
    }

    [Fact]
    public async Task Refuses_an_oversized_sqlite_file_before_reading_it()
    {
        await using (var stream = new FileStream(source.Path, FileMode.Open)) stream.SetLength(DatabaseCopier.MaxSqliteBytes + 1);

        var exception = await Assert.ThrowsAsync<MigrationAbortedException>(Copy);

        Assert.Contains("400 MB", exception.Message);
    }

    [Fact]
    public async Task Reports_values_above_the_column_limit_without_printing_them()
    {
        source.Insert("AspNetUsers", ("Id", userId), ("NomeCompleto", PersonalMarker + new string('a', 150)));

        var exception = await Assert.ThrowsAsync<MigrationAbortedException>(Copy);

        Assert.Contains("AspNetUsers.NomeCompleto: 1 valor(es) acima do tamanho máximo", exception.Message);
        Assert.DoesNotContain(PersonalMarker, exception.Message);
        Assert.Equal(0, await Scalar<long>("""SELECT COUNT(*) FROM "AspNetUsers" """));
    }

    [Fact]
    public async Task Reports_nulls_in_required_columns_and_malformed_values_by_count()
    {
        source.Insert("AspNetUsers", ("Id", userId), ("NomeCompleto", null), ("CriadoEm", "not-a-date " + PersonalMarker));

        var exception = await Assert.ThrowsAsync<MigrationAbortedException>(Copy);

        Assert.Contains("AspNetUsers.NomeCompleto: 1 valor(es) nulo(s) em coluna obrigatória", exception.Message);
        Assert.Contains("AspNetUsers.CriadoEm: 1 valor(es) em formato inválido", exception.Message);
        Assert.DoesNotContain(PersonalMarker, exception.Message);
    }

    [Fact]
    public async Task Rolls_back_everything_when_postgres_rejects_a_row_and_hides_the_row()
    {
        SeedRealisticData();
        // Parent tables copy fine; this orphan row violates a foreign key in a later table.
        source.Insert("AlunoCompetencias", ("AlunoId", userId), ("CompetenciaId", 999L));

        var exception = await Assert.ThrowsAsync<MigrationAbortedException>(Copy);

        Assert.Contains("FK_AlunoCompetencias_Competencias_CompetenciaId", exception.Message);
        Assert.DoesNotContain("Sintético", exception.Message);
        foreach (var table in SeededTables)
            Assert.Equal(0, await Scalar<long>($"SELECT COUNT(*) FROM \"{table}\""));
    }

    [Fact]
    public async Task Refuses_schemas_that_differ_in_columns_or_tables()
    {
        source.Execute("""ALTER TABLE "Alunos" ADD COLUMN "Extra" TEXT""");
        source.Execute("""ALTER TABLE "Alunos" DROP COLUMN "Bio" """);
        source.Execute("""CREATE TABLE "TabelaAntiga" ("Id" INTEGER)""");

        var exception = await Assert.ThrowsAsync<MigrationAbortedException>(Copy);

        Assert.Contains("Alunos.Extra não existe no destino", exception.Message);
        Assert.Contains("Alunos.Bio não existe na origem", exception.Message);
        Assert.Contains("tabela TabelaAntiga existe na origem", exception.Message);
    }

    [Fact]
    public async Task Does_not_copy_data_protection_keys_or_migration_history()
    {
        source.Execute("""CREATE TABLE "DataProtectionKeys" ("Id" INTEGER, "FriendlyName" TEXT, "Xml" TEXT)""");
        source.Execute("""INSERT INTO "DataProtectionKeys" VALUES (1, 'k', '<key/>')""");
        source.Execute("""INSERT INTO "__EFMigrationsHistory" VALUES ('20260101_Old', '9.0.0')""");

        await Copy();

        Assert.Equal(0, await Scalar<long>("""SELECT COUNT(*) FROM "DataProtectionKeys" """));
    }

    private void SeedRealisticData()
    {
        var roleId = SqliteFixture.NewId();
        source.Insert("AspNetRoles", ("Id", roleId), ("Name", "ALUNO"), ("NormalizedName", "ALUNO"));
        source.Insert("AspNetUsers", ("Id", userId), ("NomeCompleto", "Aluno Sintético"), ("NomeBusca", "aluno sintetico"),
            ("CriadoEm", "2026-09-11 12:22:11.123456-03:00"), ("UserName", "aluno@example.test"),
            ("NormalizedUserName", "ALUNO@EXAMPLE.TEST"), ("Email", "aluno@example.test"),
            ("NormalizedEmail", "ALUNO@EXAMPLE.TEST"), ("EmailConfirmed", 1L), ("PasswordHash", "hash-sintetico"),
            ("AccessFailedCount", 2L));
        source.Insert("AspNetUserRoles", ("UserId", userId), ("RoleId", roleId));
        source.Insert("AspNetUserClaims", ("Id", 5L), ("UserId", userId));
        source.Insert("Alunos", ("UserId", userId), ("Slug", "aluno-sintetico"), ("Ativo", 1L));
        foreach (var id in new[] { 7L, 12L, 40L })
            source.Insert("Competencias", ("Id", id), ("Nome", $"Competência {id}"), ("NomeBusca", $"competencia {id}"));
        foreach (var id in new[] { 3L, 9L })
            source.Insert("Idiomas", ("Id", id), ("Nome", $"Idioma {id}"), ("NomeBusca", $"idioma {id}"));
        source.Insert("AlunoCompetencias", ("AlunoId", userId), ("CompetenciaId", 12L));
        source.Insert("Formacoes", ("Id", SqliteFixture.NewId()), ("AlunoId", userId), ("Principal", 1L),
            ("DataInicio", "2024-02-01"), ("DataFim", "2025-12-15"));
        source.Insert("Auditorias", ("Id", SqliteFixture.NewId()), ("AdminUserId", userId),
            ("CriadoEm", "2026-09-11 12:22:11"));
    }

    private Task<IReadOnlyList<TableCount>> Copy() =>
        DatabaseCopier.CopyAsync(source.Path, target, _ => { }, CancellationToken.None);

    private async Task<T> Scalar<T>(string sql)
    {
        await using var connection = new NpgsqlConnection(target);
        await using var command = new NpgsqlCommand(sql, connection);
        await connection.OpenAsync();
        return (T)(await command.ExecuteScalarAsync())!;
    }

    private async Task<List<T>> List<T>(string sql)
    {
        await using var connection = new NpgsqlConnection(target);
        await using var command = new NpgsqlCommand(sql, connection);
        await connection.OpenAsync();
        await using var reader = await command.ExecuteReaderAsync();
        var values = new List<T>();
        while (await reader.ReadAsync()) values.Add(reader.GetFieldValue<T>(0));
        return values;
    }

    private async Task<long> NextValue(string table, string column) =>
        await Scalar<long>($"SELECT nextval(pg_get_serial_sequence('\"{table}\"', '{column}'))");

    private async Task Execute(string sql)
    {
        await using var connection = new NpgsqlConnection(target);
        await using var command = new NpgsqlCommand(sql, connection);
        await connection.OpenAsync();
        await command.ExecuteNonQueryAsync();
    }
}
