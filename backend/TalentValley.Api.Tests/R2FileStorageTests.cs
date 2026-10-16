using Amazon.S3;
using Microsoft.Extensions.Options;
using TalentValley.Api.Storage;

namespace TalentValley.Api.Tests;

public sealed class R2FileStorageTests
{
    private const string ValidKey = "0123456789abcdef0123456789abcdef.pdf";

    // The client points at an unreachable address: every case below must be rejected before any request is sent.
    private static readonly R2FileStorage Storage = new(
        new AmazonS3Client("test-key-id", "test-key-value", new AmazonS3Config { ServiceURL = "https://127.0.0.1:1" }),
        Options.Create(new StorageOptions { R2 = new R2Options { BucketName = "test-bucket" } }));

    [Theory]
    [InlineData(FileCategory.Photo, "0123456789abcdef0123456789abcdef.jpg", "fotos/0123456789abcdef0123456789abcdef.jpg")]
    [InlineData(FileCategory.Photo, "0123456789abcdef0123456789abcdef.webp", "fotos/0123456789abcdef0123456789abcdef.webp")]
    [InlineData(FileCategory.Curriculum, ValidKey, "curriculos/" + ValidKey)]
    [InlineData(FileCategory.Certificate, ValidKey, "certificados/" + ValidKey)]
    public void Object_key_is_the_category_folder_followed_by_the_storage_key(FileCategory category, string storageKey, string expected) =>
        Assert.Equal(expected, StorageKeys.ObjectKey(category, storageKey));

    [Theory]
    [InlineData("../0123456789abcdef0123456789abcdef.pdf")]
    [InlineData("curriculos/0123456789abcdef0123456789abcdef.pdf")]
    [InlineData("..\\0123456789abcdef0123456789abcdef.pdf")]
    [InlineData("C:\\secret.pdf")]
    [InlineData("0123456789abcdef0123456789abcdef.exe")]
    [InlineData("0123456789ABCDEF0123456789ABCDEF.pdf")]
    [InlineData("0123456789abcdef.pdf")]
    [InlineData("0123456789abcdef0123456789abcdef.pdf\n")]
    [InlineData("")]
    public void Object_key_rejects_keys_that_are_not_generated_names(string storageKey) =>
        Assert.Throws<ArgumentException>(() => StorageKeys.ObjectKey(FileCategory.Curriculum, storageKey));

    [Theory]
    [InlineData(FileCategory.Photo, "png", ".png")]
    [InlineData(FileCategory.Photo, ".JPG", ".jpg")]
    [InlineData(FileCategory.Certificate, "PDF", ".pdf")]
    public void Extension_is_normalized_per_category(FileCategory category, string extension, string expected) =>
        Assert.Equal(expected, StorageKeys.NormalizeExtension(category, extension));

    [Theory]
    [InlineData(FileCategory.Photo, ".pdf")]
    [InlineData(FileCategory.Photo, ".gif")]
    [InlineData(FileCategory.Curriculum, ".jpg")]
    [InlineData(FileCategory.Certificate, ".exe")]
    [InlineData(FileCategory.Certificate, "")]
    public void Extension_outside_the_category_whitelist_is_rejected(FileCategory category, string extension) =>
        Assert.Throws<ArgumentException>(() => StorageKeys.NormalizeExtension(category, extension));

    [Fact]
    public async Task Storage_rejects_invalid_keys_and_extensions_without_calling_the_bucket()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => Storage.OpenReadAsync(FileCategory.Photo, "../secret.jpg"));
        await Assert.ThrowsAsync<ArgumentException>(() => Storage.ExistsAsync(FileCategory.Photo, "C:\\secret.jpg"));
        await Assert.ThrowsAsync<ArgumentException>(() => Storage.DeleteAsync(FileCategory.Certificate, "other/" + ValidKey));
        await Assert.ThrowsAsync<ArgumentException>(() => Storage.StoreAsync(FileCategory.Certificate,
            new MemoryStream([1]), ".jpg"));
    }

    [Fact]
    public void R2_settings_are_validated_without_echoing_their_values()
    {
        var options = new R2Options
        {
            AccountId = "not a valid host!", BucketName = "Bad_Bucket", AccessKeyId = " ", SecretAccessKey = "super-secret-value"
        };
        var exception = Assert.Throws<InvalidOperationException>(options.Validate);
        Assert.Contains("Storage:R2:AccountId", exception.Message);
        Assert.Contains("Storage:R2:BucketName", exception.Message);
        Assert.Contains("Storage:R2:AccessKeyId", exception.Message);
        Assert.DoesNotContain("Storage:R2:SecretAccessKey", exception.Message);
        Assert.DoesNotContain("super-secret-value", exception.Message);
        Assert.DoesNotContain("not a valid host", exception.Message);
    }

    [Theory]
    [InlineData("testaccount\n", "test-bucket", "Storage:R2:AccountId")]
    [InlineData("testaccount", "test-bucket\n", "Storage:R2:BucketName")]
    public void R2_settings_with_a_trailing_line_break_are_rejected(string accountId, string bucketName, string expected)
    {
        var options = new R2Options { AccountId = accountId, BucketName = bucketName, AccessKeyId = "id", SecretAccessKey = "value" };
        Assert.Contains(expected, Assert.Throws<InvalidOperationException>(options.Validate).Message);
    }

    [Fact]
    public void R2_endpoint_is_derived_from_the_account_id() =>
        Assert.Equal("https://abc123.r2.cloudflarestorage.com", new R2Options { AccountId = "abc123" }.ServiceUrl);
}
