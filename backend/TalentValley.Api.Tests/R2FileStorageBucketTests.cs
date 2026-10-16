using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Amazon.Runtime;
using Amazon.S3;
using Microsoft.Extensions.Options;
using TalentValley.Api.Storage;

namespace TalentValley.Api.Tests;

// Runs the real AWS SDK against an in-memory S3 endpoint, so the request flow of R2FileStorage is covered without a bucket.
public sealed class R2FileStorageBucketTests : IDisposable
{
    private const string Bucket = "test-bucket";
    private readonly FakeBucket bucket = new(Bucket);
    private readonly AmazonS3Client client;
    private readonly R2FileStorage storage;

    public R2FileStorageBucketTests()
    {
        client = new AmazonS3Client("test-key-id", "test-key-value", new AmazonS3Config
        {
            ServiceURL = "https://account.r2.example.test",
            ForcePathStyle = true,
            AuthenticationRegion = "auto",
            MaxErrorRetry = 0,
            HttpClientFactory = new FakeBucketClientFactory(bucket)
        });
        storage = CreateStorage(Bucket);
    }

    public void Dispose() => client.Dispose();

    [Fact]
    public async Task Store_read_exists_and_delete_round_trip_through_the_bucket()
    {
        var bytes = "%PDF-test"u8.ToArray();
        var key = await storage.StoreAsync(FileCategory.Certificate, new MemoryStream(bytes), "PDF");

        Assert.Matches("^[a-f0-9]{32}\\.pdf$", key);
        Assert.Equal(bytes, bucket.Objects[$"{Bucket}/certificados/{key}"]);
        Assert.True(await storage.ExistsAsync(FileCategory.Certificate, key));
        Assert.False(await storage.ExistsAsync(FileCategory.Curriculum, key));

        await using (var read = await storage.OpenReadAsync(FileCategory.Certificate, key))
        {
            Assert.NotNull(read);
            using var copy = new MemoryStream();
            await read.CopyToAsync(copy);
            Assert.Equal(bytes, copy.ToArray());
        }

        await storage.DeleteAsync(FileCategory.Certificate, key);
        await storage.DeleteAsync(FileCategory.Certificate, key);
        Assert.False(await storage.ExistsAsync(FileCategory.Certificate, key));
        Assert.Null(await storage.OpenReadAsync(FileCategory.Certificate, key));
    }

    [Fact]
    public async Task Stored_keys_are_unique_and_isolated_by_category_folder()
    {
        var first = await storage.StoreAsync(FileCategory.Photo, new MemoryStream([1]), ".png");
        var second = await storage.StoreAsync(FileCategory.Photo, new MemoryStream([2]), ".png");

        Assert.NotEqual(first, second);
        Assert.Equal(2, bucket.Objects.Keys.Count(x => x.StartsWith($"{Bucket}/fotos/", StringComparison.Ordinal)));
        Assert.Null(await storage.OpenReadAsync(FileCategory.Curriculum, first));
    }

    [Fact]
    public async Task A_missing_bucket_surfaces_as_an_error_instead_of_a_missing_file()
    {
        var wrongBucket = CreateStorage("other-bucket");
        const string key = "0123456789abcdef0123456789abcdef.pdf";

        await Assert.ThrowsAsync<AmazonS3Exception>(() => wrongBucket.OpenReadAsync(FileCategory.Certificate, key));
    }

    [Fact]
    public void Unknown_categories_have_no_folder() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => StorageKeys.CategoryFolder((FileCategory)99));

    private R2FileStorage CreateStorage(string bucketName) =>
        new(client, Options.Create(new StorageOptions { R2 = new R2Options { BucketName = bucketName } }));

    private sealed class FakeBucketClientFactory(FakeBucket bucket) : HttpClientFactory
    {
        public override HttpClient CreateHttpClient(IClientConfig clientConfig) => new(bucket);
    }

    private sealed class FakeBucket(string name) : HttpMessageHandler
    {
        public ConcurrentDictionary<string, byte[]> Objects { get; } = new();

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = Uri.UnescapeDataString(request.RequestUri!.AbsolutePath.TrimStart('/'));
            if (path.Split('/')[0] != name) return Error("NoSuchBucket");

            switch (request.Method.Method)
            {
                case "PUT":
                    Objects[path] = await request.Content!.ReadAsByteArrayAsync(cancellationToken);
                    var created = new HttpResponseMessage(HttpStatusCode.OK);
                    created.Headers.ETag = new EntityTagHeaderValue("\"etag\"");
                    return created;
                case "GET" when Objects.TryGetValue(path, out var content):
                    return new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(content) };
                case "GET":
                    return Error("NoSuchKey");
                case "HEAD":
                    return new HttpResponseMessage(Objects.ContainsKey(path) ? HttpStatusCode.OK : HttpStatusCode.NotFound);
                case "DELETE":
                    Objects.TryRemove(path, out _);
                    return new HttpResponseMessage(HttpStatusCode.NoContent);
                default:
                    return new HttpResponseMessage(HttpStatusCode.MethodNotAllowed);
            }
        }

        private static HttpResponseMessage Error(string code) => new(HttpStatusCode.NotFound)
        {
            Content = new StringContent(
                $"<?xml version=\"1.0\" encoding=\"UTF-8\"?><Error><Code>{code}</Code><Message>{code}</Message></Error>",
                Encoding.UTF8, "application/xml")
        };
    }
}
