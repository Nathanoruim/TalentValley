using Amazon.S3;
using Amazon.S3.Model;
using TalentValley.Api.Storage;

namespace SqliteToPostgresMigrator;

internal sealed class S3ObjectStore(IAmazonS3 client, string bucketName) : IObjectStore
{
    public async Task<IReadOnlyDictionary<string, long>> ListAsync(CancellationToken cancellationToken)
    {
        var objects = new Dictionary<string, long>(StringComparer.Ordinal);
        foreach (var category in Enum.GetValues<FileCategory>())
        {
            var request = new ListObjectsV2Request
            {
                BucketName = bucketName,
                Prefix = $"{StorageKeys.CategoryFolder(category)}/"
            };
            ListObjectsV2Response response;
            do
            {
                response = await client.ListObjectsV2Async(request, cancellationToken);
                foreach (var item in response.S3Objects ?? [])
                    objects[item.Key] = item.Size ?? 0;
                request.ContinuationToken = response.NextContinuationToken;
            } while (response.IsTruncated == true);
        }
        return objects;
    }

    public async Task PutAsync(string objectKey, string filePath, CancellationToken cancellationToken)
    {
        await client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = bucketName,
            Key = objectKey,
            FilePath = filePath,
            // Same settings as R2FileStorage: R2 does not accept the SDK's default streaming signature.
            DisablePayloadSigning = true,
            DisableDefaultChecksumValidation = true
        }, cancellationToken);
    }
}
