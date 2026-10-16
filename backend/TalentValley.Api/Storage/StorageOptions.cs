namespace TalentValley.Api.Storage;

public sealed class StorageOptions
{
    public const string SectionName = "Storage";
    public const string LocalProvider = "Local";
    public const string R2Provider = "R2";

    // "R2" uses Cloudflare R2; absent or "Local" uses the disk (Development and tests only).
    public string? Provider { get; init; }
    public string RootPath { get; init; } = "App_Data/storage";
    public R2Options R2 { get; init; } = new();
}
