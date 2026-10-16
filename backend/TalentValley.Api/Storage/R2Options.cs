using System.Text.RegularExpressions;

namespace TalentValley.Api.Storage;

public sealed partial class R2Options
{
    public string? AccountId { get; init; }
    public string? BucketName { get; init; }
    public string? AccessKeyId { get; init; }
    public string? SecretAccessKey { get; init; }

    public string ServiceUrl => $"https://{AccountId}.r2.cloudflarestorage.com";

    // Names the missing or malformed settings only; values (especially credentials) never reach the message.
    public void Validate()
    {
        var invalid = new List<string>();
        if (string.IsNullOrWhiteSpace(AccountId) || !AccountIdPattern().IsMatch(AccountId))
            invalid.Add("Storage:R2:AccountId");
        if (string.IsNullOrWhiteSpace(BucketName) || !BucketNamePattern().IsMatch(BucketName))
            invalid.Add("Storage:R2:BucketName");
        if (string.IsNullOrWhiteSpace(AccessKeyId)) invalid.Add("Storage:R2:AccessKeyId");
        if (string.IsNullOrWhiteSpace(SecretAccessKey)) invalid.Add("Storage:R2:SecretAccessKey");
        if (invalid.Count > 0)
            throw new InvalidOperationException(
                $"Storage:Provider is R2, but these settings are missing or invalid: {string.Join(", ", invalid)}.");
    }

    [GeneratedRegex("^[a-zA-Z0-9]{1,64}\\z", RegexOptions.CultureInvariant)]
    private static partial Regex AccountIdPattern();

    [GeneratedRegex("^[a-z0-9][a-z0-9-]{1,61}[a-z0-9]\\z", RegexOptions.CultureInvariant)]
    private static partial Regex BucketNamePattern();
}
