using TalentValley.Api.Tests;

namespace SqliteToPostgresMigrator.Tests;

public sealed class FileCopierTests : IAsyncLifetime
{
    private static readonly string Photo = new string('a', 32) + ".jpg";
    private static readonly string Curriculum = new string('b', 32) + ".pdf";
    private static readonly string Certificate = new string('c', 32) + ".pdf";
    private static readonly string MissingCurriculum = new string('d', 32) + ".pdf";
    private static readonly string UnreferencedCertificate = new string('e', 32) + ".pdf";

    private readonly string storage = Directory.CreateTempSubdirectory("tv-migrator-storage-").FullName;
    private readonly InMemoryObjectStore store = new();
    private string target = string.Empty;
    private SqliteFixture source = null!;

    public async Task InitializeAsync()
    {
        target = PostgresTestDatabase.CreateDatabase();
        source = await SqliteFixture.CreateAsync(target);

        var userId = SqliteFixture.NewId();
        source.Insert("AspNetUsers", ("Id", userId));
        source.Insert("Alunos", ("UserId", userId), ("FotoStorageKey", Photo), ("CurriculoStorageKey", MissingCurriculum));
        source.Insert("Formacoes", ("Id", SqliteFixture.NewId()), ("AlunoId", userId), ("CertificadoStorageKey", Certificate));

        Write("fotos", Photo, "photo-bytes");
        Write("curriculos", Curriculum, "cv-bytes");
        Write("certificados", Certificate, "certificate-bytes");
        Write("certificados", UnreferencedCertificate, "orphan-bytes");
        Write("fotos", "notes.txt", "not an upload");
        Write("fotos", new string('f', 32) + ".pdf", "a pdf cannot be a photo");
    }

    public Task DisposeAsync()
    {
        source.Dispose();
        PostgresTestDatabase.DropDatabase(target);
        Directory.Delete(storage, recursive: true);
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Uploads_valid_files_with_the_api_object_keys_and_reports_orphans_both_ways()
    {
        var foreign = "fotos/" + new string('9', 32) + ".png";
        store.Objects[foreign] = [1];

        var report = await Copy();

        Assert.Equal(
            new[]
            {
                $"certificados/{Certificate}", $"certificados/{UnreferencedCertificate}", $"curriculos/{Curriculum}",
                foreign, $"fotos/{Photo}"
            },
            store.Objects.Keys.Order(StringComparer.Ordinal));
        Assert.Equal("photo-bytes"u8.ToArray(), store.Objects[$"fotos/{Photo}"]);
        Assert.Equal(4, report.LocalFiles);
        Assert.Equal(4, report.Uploaded);
        Assert.Equal(2, report.IgnoredFiles);
        Assert.Equal(3, report.ReferencedKeys);
        Assert.Equal(1, report.ReferencedWithoutLocalFile);
        Assert.Equal(2, report.LocalFilesWithoutReference);
        Assert.Equal(1, report.BucketObjectsNotInSource);
        Assert.Equal(5, report.BucketObjects);
        Assert.True(report.HasWarnings);
    }

    [Fact]
    public async Task A_second_run_sends_nothing_again()
    {
        await Copy();

        var report = await Copy();

        Assert.Equal(0, report.Uploaded);
        Assert.Equal(4, report.AlreadyInBucket);
        Assert.Equal(4, store.PutCount);
    }

    [Fact]
    public async Task A_file_that_changed_size_is_sent_again()
    {
        await Copy();
        Write("fotos", Photo, "photo-bytes-edited");

        var report = await Copy();

        Assert.Equal(1, report.Uploaded);
        Assert.Equal("photo-bytes-edited"u8.ToArray(), store.Objects[$"fotos/{Photo}"]);
    }

    [Fact]
    public async Task Aborts_when_an_uploaded_file_is_not_confirmed_in_the_bucket()
    {
        store.DropWrites = true;

        var exception = await Assert.ThrowsAsync<MigrationAbortedException>(Copy);

        Assert.Contains("não foram confirmados", exception.Message);
    }

    [Fact]
    public async Task Aborts_when_the_storage_folder_is_missing()
    {
        var exception = await Assert.ThrowsAsync<MigrationAbortedException>(() =>
            FileCopier.CopyAsync(source.Path, Path.Combine(storage, "nope"), store, _ => { }, CancellationToken.None));

        Assert.Contains("Pasta de arquivos", exception.Message);
    }

    [Fact]
    public async Task Aborts_on_a_storage_key_column_it_does_not_know()
    {
        source.Execute("""CREATE TABLE "Extras" ("DocumentoStorageKey" TEXT)""");
        source.Execute($"INSERT INTO \"Extras\" VALUES ('{Certificate}')");
        var exception = await Assert.ThrowsAsync<MigrationAbortedException>(Copy);

        Assert.Contains("Extras.DocumentoStorageKey", exception.Message);
    }

    private Task<FileReport> Copy() =>
        FileCopier.CopyAsync(source.Path, storage, store, _ => { }, CancellationToken.None);

    private void Write(string folder, string name, string content)
    {
        Directory.CreateDirectory(Path.Combine(storage, folder));
        File.WriteAllText(Path.Combine(storage, folder, name), content);
    }

    private sealed class InMemoryObjectStore : IObjectStore
    {
        public Dictionary<string, byte[]> Objects { get; } = [];
        public bool DropWrites { get; set; }
        public int PutCount { get; private set; }

        public Task<IReadOnlyDictionary<string, long>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<string, long>>(Objects.ToDictionary(x => x.Key, x => (long)x.Value.Length));

        public async Task PutAsync(string objectKey, string filePath, CancellationToken cancellationToken)
        {
            PutCount++;
            if (!DropWrites) Objects[objectKey] = await File.ReadAllBytesAsync(filePath, cancellationToken);
        }
    }
}
