using System.Net;
using System.Text;
using System.Web;
using Amazon.Runtime;
using Amazon.S3;
using TalentValley.Api.Storage;

namespace SqliteToPostgresMigrator.Tests;

// Runs the real AWS SDK against an in-memory S3 endpoint; the list endpoint serves one object per page to exercise paging.
public sealed class S3ObjectStoreTests : IDisposable
{
    private const string Bucket = "test-bucket";
    private readonly FakeBucket bucket = new();
    private readonly AmazonS3Client client;
    private readonly S3ObjectStore store;
    private readonly string directory = Directory.CreateTempSubdirectory("tv-s3-").FullName;

    public S3ObjectStoreTests()
    {
        client = new AmazonS3Client("test-key-id", "test-key-value", new AmazonS3Config
        {
            ServiceURL = "https://account.r2.example.test",
            ForcePathStyle = true,
            AuthenticationRegion = "auto",
            MaxErrorRetry = 0,
            HttpClientFactory = new FakeBucketClientFactory(bucket)
        });
        store = new S3ObjectStore(client, Bucket);
    }

    public void Dispose()
    {
        client.Dispose();
        Directory.Delete(directory, recursive: true);
    }

    [Fact]
    public async Task Put_uploads_the_file_content_under_the_object_key()
    {
        var path = Path.Combine(directory, "file.pdf");
        await File.WriteAllBytesAsync(path, "%PDF-test"u8.ToArray());
        var key = $"certificados/{new string('a', 32)}.pdf";

        await store.PutAsync(key, path, CancellationToken.None);

        Assert.Equal("%PDF-test"u8.ToArray(), bucket.Objects[$"{Bucket}/{key}"]);
    }

    [Fact]
    public async Task List_follows_continuation_tokens_and_only_covers_the_category_folders()
    {
        var keys = new[]
        {
            $"fotos/{new string('1', 32)}.jpg", $"fotos/{new string('2', 32)}.png",
            $"curriculos/{new string('3', 32)}.pdf", $"certificados/{new string('4', 32)}.pdf"
        };
        foreach (var key in keys) bucket.Objects[$"{Bucket}/{key}"] = new byte[key.Length];
        bucket.Objects[$"{Bucket}/outros/{new string('5', 32)}.pdf"] = [1];

        var listed = await store.ListAsync(CancellationToken.None);

        Assert.Equal(keys.Order(StringComparer.Ordinal), listed.Keys.Order(StringComparer.Ordinal));
        Assert.All(listed, x => Assert.Equal(x.Key.Length, x.Value));
        Assert.True(bucket.ListRequests > 3);
    }

    private sealed class FakeBucketClientFactory(FakeBucket bucket) : HttpClientFactory
    {
        public override HttpClient CreateHttpClient(IClientConfig clientConfig) => new(bucket);
    }

    private sealed class FakeBucket : HttpMessageHandler
    {
        public Dictionary<string, byte[]> Objects { get; } = [];
        public int ListRequests { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = Uri.UnescapeDataString(request.RequestUri!.AbsolutePath.Trim('/'));
            if (request.Method == HttpMethod.Put)
            {
                Objects[path] = await request.Content!.ReadAsByteArrayAsync(cancellationToken);
                var created = new HttpResponseMessage(HttpStatusCode.OK);
                created.Headers.ETag = new System.Net.Http.Headers.EntityTagHeaderValue("\"etag\"");
                return created;
            }
            if (request.Method == HttpMethod.Get && path == Bucket) return List(HttpUtility.ParseQueryString(request.RequestUri.Query));
            return new HttpResponseMessage(HttpStatusCode.MethodNotAllowed);
        }

        private HttpResponseMessage List(System.Collections.Specialized.NameValueCollection query)
        {
            ListRequests++;
            var prefix = query["prefix"] ?? string.Empty;
            var matching = Objects.Where(x => x.Key.StartsWith($"{Bucket}/{prefix}", StringComparison.Ordinal))
                .OrderBy(x => x.Key, StringComparer.Ordinal).ToList();
            var start = int.TryParse(query["continuation-token"], out var token) ? token : 0;
            var page = matching.Skip(start).Take(1).ToList();
            var truncated = start + 1 < matching.Count;

            var xml = new StringBuilder("<?xml version=\"1.0\" encoding=\"UTF-8\"?>")
                .Append("<ListBucketResult xmlns=\"http://s3.amazonaws.com/doc/2006-03-01/\">")
                .Append($"<Name>{Bucket}</Name><Prefix>{prefix}</Prefix><KeyCount>{page.Count}</KeyCount><MaxKeys>1</MaxKeys>")
                .Append($"<IsTruncated>{truncated.ToString().ToLowerInvariant()}</IsTruncated>");
            if (truncated) xml.Append($"<NextContinuationToken>{start + 1}</NextContinuationToken>");
            foreach (var item in page)
                xml.Append($"<Contents><Key>{item.Key[(Bucket.Length + 1)..]}</Key><Size>{item.Value.Length}</Size></Contents>");
            xml.Append("</ListBucketResult>");

            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(xml.ToString(), Encoding.UTF8, "application/xml") };
        }
    }
}
