using TalentValley.Api.Storage;

namespace SqliteToPostgresMigrator;

internal sealed record FileReport(
    int LocalFiles,
    int IgnoredFiles,
    int Uploaded,
    int AlreadyInBucket,
    int ReferencedKeys,
    int InvalidReferences,
    int ReferencedWithoutLocalFile,
    int LocalFilesWithoutReference,
    int BucketObjects,
    int BucketObjectsNotInSource)
{
    // Orphans in either direction do not stop the migration, but the maintainer must see them.
    public bool HasWarnings => IgnoredFiles + InvalidReferences + ReferencedWithoutLocalFile + LocalFilesWithoutReference > 0;
}

// Copies the private files of the Railway volume to the bucket, keeping the "folder/name" keys the API expects.
internal static class FileCopier
{
    private static readonly (string Column, FileCategory Category)[] KeyColumns =
    [
        ("FotoStorageKey", FileCategory.Photo),
        ("CurriculoStorageKey", FileCategory.Curriculum),
        ("CertificadoStorageKey", FileCategory.Certificate)
    ];

    public static async Task<FileReport> CopyAsync(
        string sqlitePath, string storageDirectory, IObjectStore store, Action<string> log,
        CancellationToken cancellationToken)
    {
        if (!Directory.Exists(storageDirectory))
            throw new MigrationAbortedException("Pasta de arquivos não encontrada.");

        var (referenced, invalidReferences) = ReadReferences(sqlitePath);
        var (local, ignored) = ReadLocalFiles(storageDirectory);
        var inBucket = await store.ListAsync(cancellationToken);

        var uploaded = 0;
        var skipped = 0;
        foreach (var (objectKey, file) in local.OrderBy(x => x.Key, StringComparer.Ordinal))
        {
            if (inBucket.TryGetValue(objectKey, out var existingSize) && existingSize == file.Length)
            {
                skipped++;
                continue;
            }
            await store.PutAsync(objectKey, file.FullName, cancellationToken);
            uploaded++;
        }
        log($"Arquivos enviados: {uploaded}; já no bucket: {skipped}.");

        var afterUpload = await store.ListAsync(cancellationToken);
        var unconfirmed = local.Count(x => !afterUpload.TryGetValue(x.Key, out var size) || size != x.Value.Length);
        if (unconfirmed > 0)
            throw new MigrationAbortedException($"{unconfirmed} arquivo(s) não foram confirmados no bucket após o envio.");

        return new FileReport(
            LocalFiles: local.Count,
            IgnoredFiles: ignored,
            Uploaded: uploaded,
            AlreadyInBucket: skipped,
            ReferencedKeys: referenced.Count,
            InvalidReferences: invalidReferences,
            ReferencedWithoutLocalFile: referenced.Count(x => !local.ContainsKey(x)),
            LocalFilesWithoutReference: local.Keys.Count(x => !referenced.Contains(x)),
            BucketObjects: afterUpload.Count,
            BucketObjectsNotInSource: afterUpload.Keys.Count(x => !local.ContainsKey(x)));
    }

    // Object keys ("folder/name") referenced by the database, found from the *StorageKey columns of whatever tables hold them.
    private static (HashSet<string> Keys, int Invalid) ReadReferences(string sqlitePath)
    {
        using var source = SqliteSource.Open(sqlitePath);
        var keys = new HashSet<string>(StringComparer.Ordinal);
        var invalid = 0;

        foreach (var table in source.TableNames())
        {
            foreach (var column in source.ColumnNames(table).Where(x => x.EndsWith("StorageKey", StringComparison.Ordinal)))
            {
                var category = KeyColumns.SingleOrDefault(x => x.Column == column);
                if (category.Column is null)
                    throw new MigrationAbortedException($"Coluna de arquivo desconhecida: {table}.{column}.");

                foreach (var value in source.DistinctValues(table, column))
                {
                    if (TryObjectKey(category.Category, value, out var objectKey)) keys.Add(objectKey);
                    else invalid++;
                }
            }
        }
        return (keys, invalid);
    }

    private static (Dictionary<string, FileInfo> Files, int Ignored) ReadLocalFiles(string storageDirectory)
    {
        var files = new Dictionary<string, FileInfo>(StringComparer.Ordinal);
        var ignored = 0;

        foreach (var category in Enum.GetValues<FileCategory>())
        {
            var directory = new DirectoryInfo(Path.Combine(storageDirectory, StorageKeys.CategoryFolder(category)));
            if (!directory.Exists) continue;
            foreach (var file in directory.EnumerateFiles())
            {
                if (TryObjectKey(category, file.Name, out var objectKey)) files[objectKey] = file;
                else ignored++;
            }
        }
        return (files, ignored);
    }

    // Only keys the API itself could have generated for the category are copied or counted as references.
    private static bool TryObjectKey(FileCategory category, string storageKey, out string objectKey)
    {
        objectKey = string.Empty;
        try
        {
            StorageKeys.NormalizeExtension(category, Path.GetExtension(storageKey));
            objectKey = StorageKeys.ObjectKey(category, storageKey);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
