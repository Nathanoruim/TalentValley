using System.Net;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;

namespace TalentValley.Api.Storage;

public sealed class R2FileStorage : IFileStorage
{
    private readonly IAmazonS3 client;
    private readonly string bucketName;

    public R2FileStorage(IAmazonS3 client, IOptions<StorageOptions> options)
    {
        this.client = client;
        bucketName = options.Value.R2.BucketName
            ?? throw new InvalidOperationException("Storage:R2:BucketName must be configured.");
    }

    public static IAmazonS3 CreateClient(R2Options options)
    {
        options.Validate();
        return new AmazonS3Client(
            new BasicAWSCredentials(options.AccessKeyId, options.SecretAccessKey),
            new AmazonS3Config
            {
                ServiceURL = options.ServiceUrl,
                ForcePathStyle = true,
                AuthenticationRegion = "auto",
                // Recent SDKs add checksums by default that R2 does not accept on every operation.
                RequestChecksumCalculation = RequestChecksumCalculation.WHEN_REQUIRED,
                ResponseChecksumValidation = ResponseChecksumValidation.WHEN_REQUIRED
            });
    }

    public async Task<string> StoreAsync(FileCategory category, Stream content, string extension,
        CancellationToken cancellationToken = default)
    {
        var key = StorageKeys.NewKey(category, extension);

        // Uploads are capped at 10 MB by FileUploadValidator; buffering gives the request a known length.
        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        buffer.Position = 0;

        await client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = bucketName,
            Key = StorageKeys.ObjectKey(category, key),
            InputStream = buffer,
            AutoCloseStream = false,
            // R2 does not implement the streaming SigV4 upload the SDK uses by default; HTTPS protects the payload.
            DisablePayloadSigning = true,
            DisableDefaultChecksumValidation = true
        }, cancellationToken);
        return key;
    }

    public async Task<Stream?> OpenReadAsync(FileCategory category, string storageKey,
        CancellationToken cancellationToken = default)
    {
        var objectKey = StorageKeys.ObjectKey(category, storageKey);
        try
        {
            var response = await client.GetObjectAsync(bucketName, objectKey, cancellationToken);
            return response.ResponseStream;
        }
        catch (AmazonS3Exception exception) when (exception.ErrorCode == "NoSuchKey")
        {
            // A missing bucket (NoSuchBucket) is a configuration error and must surface instead of looking like a missing file.
            return null;
        }
    }

    public async Task<bool> ExistsAsync(FileCategory category, string storageKey,
        CancellationToken cancellationToken = default)
    {
        var objectKey = StorageKeys.ObjectKey(category, storageKey);
        try
        {
            await client.GetObjectMetadataAsync(bucketName, objectKey, cancellationToken);
            return true;
        }
        catch (AmazonS3Exception exception) when (exception.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    // DeleteObject succeeds when the object is already gone, which keeps the call idempotent.
    public async Task DeleteAsync(FileCategory category, string storageKey,
        CancellationToken cancellationToken = default) =>
        await client.DeleteObjectAsync(bucketName, StorageKeys.ObjectKey(category, storageKey), cancellationToken);
}
