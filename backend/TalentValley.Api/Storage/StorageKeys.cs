using System.Text.RegularExpressions;

namespace TalentValley.Api.Storage;

// Rules shared by every IFileStorage implementation, so a key valid for one provider is valid for the others.
public static partial class StorageKeys
{
    public static string NewKey(FileCategory category, string extension) =>
        $"{Guid.NewGuid():N}{NormalizeExtension(category, extension)}";

    public static string CategoryFolder(FileCategory category) => category switch
    {
        FileCategory.Photo => "fotos",
        FileCategory.Curriculum => "curriculos",
        FileCategory.Certificate => "certificados",
        _ => throw new ArgumentOutOfRangeException(nameof(category))
    };

    public static string NormalizeExtension(FileCategory category, string extension)
    {
        var normalized = extension.StartsWith('.') ? extension.ToLowerInvariant() : $".{extension.ToLowerInvariant()}";
        var allowed = category == FileCategory.Photo
            ? normalized is ".jpg" or ".png" or ".webp"
            : normalized == ".pdf";
        return allowed ? normalized : throw new ArgumentException("Invalid extension.", nameof(extension));
    }

    public static string ValidateKey(string storageKey) =>
        StorageKeyPattern().IsMatch(storageKey)
            ? storageKey
            : throw new ArgumentException("Invalid storage key.", nameof(storageKey));

    // Object name inside a bucket: "<category folder>/<storage key>".
    public static string ObjectKey(FileCategory category, string storageKey) =>
        $"{CategoryFolder(category)}/{ValidateKey(storageKey)}";

    [GeneratedRegex("^[a-f0-9]{32}\\.(jpg|png|webp|pdf)\\z", RegexOptions.CultureInvariant)]
    private static partial Regex StorageKeyPattern();
}
