using System.Net;
using System.Text;
using Eudiplo.Client.Tests.TestSupport;

namespace Eudiplo.Client.Tests;

public class EudiploApiClientStorageTests
{
    [Fact]
    public async Task UploadAsync_Success_ReturnsResponseBody()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.OK, """{"key":"abc123"}""");

        using var content = new MemoryStream(Encoding.UTF8.GetBytes("fake image bytes"));
        var result = await client.UploadAsync(content, "logo.png", "image/png");

        Assert.Contains("abc123", result);
        Assert.Equal(HttpMethod.Post, handler.Requests[1].Method);
        Assert.Equal("/api/storage", handler.Requests[1].RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task UploadAsync_Failure_Throws()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.BadRequest, """{"message":"unsupported format"}""");

        using var content = new MemoryStream(Encoding.UTF8.GetBytes("data"));
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.UploadAsync(content, "file.exe", "application/octet-stream"));
    }

    [Fact]
    public async Task UploadAsync_RetriedAfter401_ResendsFullStreamContent()
    {
        // Regression guard: the stream must be rewound before the retried send, otherwise the
        // second attempt would transmit an empty body.
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken("tok-1");
        handler.Enqueue(HttpStatusCode.Unauthorized);
        handler.EnqueueToken("tok-2");
        handler.Enqueue(HttpStatusCode.OK, """{"key":"abc123"}""");

        using var content = new MemoryStream(Encoding.UTF8.GetBytes("payload-bytes"));
        await client.UploadAsync(content, "file.bin", "application/octet-stream");

        // Both the initial (401) and retried (200) attempt must have carried the full body
        // (multipart-encoded, so check for the payload as a substring of the envelope).
        Assert.Contains("payload-bytes", handler.Requests[1].Body);
        Assert.Contains("payload-bytes", handler.Requests[3].Body);
    }

    [Fact]
    public async Task DownloadAsync_Success_ReturnsBytes()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent([1, 2, 3, 4]),
        });

        var result = await client.DownloadAsync("abc123");

        Assert.Equal(new byte[] { 1, 2, 3, 4 }, result);
        Assert.Equal("/api/storage/abc123", handler.Requests[1].RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task DownloadAsync_NotFound_ReturnsNull()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.NotFound);

        Assert.Null(await client.DownloadAsync("missing"));
    }
}
