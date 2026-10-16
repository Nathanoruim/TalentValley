using Microsoft.Extensions.Options;

namespace TalentValley.Api.Storage;

public sealed class LocalFileStorage : IFileStorage
{
    private readonly string rootPath;

    public LocalFileStorage(IOptions<StorageOptions> options, IWebHostEnvironment environment)
    {
        var configured = options.Value.RootPath;
        if (string.IsNullOrWhiteSpace(configured))
            throw new InvalidOperationException("Storage:RootPath must be configured.");

        rootPath = Path.GetFullPath(Path.IsPathRooted(configured)
            ? configured
            : Path.Combine(environment.ContentRootPath, configured));
    }

    public async Task<string> StoreAsync(FileCategory category, Stream content, string extension,
        CancellationToken cancellationToken = default)
    {
        var key = StorageKeys.NewKey(category, extension);
        Directory.CreateDirectory(CategoryDirectory(category));
        var path = ResolvePath(category, key);
        try
        {
            await using var destination = new FileStream(path, FileMode.CreateNew, FileAccess.Write,
                FileShare.None, 81920, FileOptions.Asynchronous);
            await content.CopyToAsync(destination, cancellationToken);
            return key;
        }
        catch
        {
            if (File.Exists(path)) File.Delete(path);
            throw;
        }
    }

    public Task<Stream?> OpenReadAsync(FileCategory category, string storageKey,
        CancellationToken cancellationToken = default)
    {
        var path = ResolvePath(category, storageKey);
        Stream? stream = File.Exists(path)
            ? new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920,
                FileOptions.Asynchronous | FileOptions.SequentialScan)
            : null;
        return Task.FromResult(stream);
    }

    public Task<bool> ExistsAsync(FileCategory category, string storageKey,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(File.Exists(ResolvePath(category, storageKey)));

    public Task DeleteAsync(FileCategory category, string storageKey,
        CancellationToken cancellationToken = default)
    {
        var path = ResolvePath(category, storageKey);
        if (File.Exists(path)) File.Delete(path);
        return Task.CompletedTask;
    }

    private string ResolvePath(FileCategory category, string storageKey)
    {
        StorageKeys.ValidateKey(storageKey);

        var directory = CategoryDirectory(category);
        var path = Path.GetFullPath(Path.Combine(directory, storageKey));
        if (!path.StartsWith(directory + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Invalid storage key.", nameof(storageKey));
        return path;
    }

    private string CategoryDirectory(FileCategory category) =>
        Path.Combine(rootPath, StorageKeys.CategoryFolder(category));
}
