using System.Net;
using Eudiplo.Client.Tests.TestSupport;

namespace Eudiplo.Client.Tests;

public class EudiploApiClientCacheTests
{
    [Fact]
    public async Task GetCacheStatsAsync_NonSuccessStatus_ReturnsNull()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.InternalServerError);

        Assert.Null(await client.GetCacheStatsAsync());
    }

    [Fact]
    public async Task GetCacheStatsAsync_Success_ReturnsBody()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.OK, """{"entries":42}""");

        var result = await client.GetCacheStatsAsync();

        Assert.Contains("42", result);
        Assert.Equal("/api/cache/stats", handler.Requests[1].RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task ClearAllCachesAsync_Failure_Throws()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.InternalServerError);

        await Assert.ThrowsAsync<InvalidOperationException>(() => client.ClearAllCachesAsync());
    }

    [Fact]
    public async Task ClearTrustListCacheAsync_Success_SendsExpectedPath()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.OK);

        await client.ClearTrustListCacheAsync();

        Assert.Equal("/api/cache/trust-list", handler.Requests[1].RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task ClearStatusListCacheAsync_Success_SendsExpectedPath()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.OK);

        await client.ClearStatusListCacheAsync();

        Assert.Equal("/api/cache/status-list", handler.Requests[1].RequestUri!.AbsolutePath);
    }
}
