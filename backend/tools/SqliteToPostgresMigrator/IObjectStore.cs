namespace SqliteToPostgresMigrator;

// The slice of the bucket the migration needs; keeps the file copy testable without a real R2 account.
internal interface IObjectStore
{
    // Every object under the three category folders, as "folder/name" -> size in bytes.
    Task<IReadOnlyDictionary<string, long>> ListAsync(CancellationToken cancellationToken);

    Task PutAsync(string objectKey, string filePath, CancellationToken cancellationToken);
}
