using System.Net;
using Eudiplo.Client.Tests.TestSupport;

namespace Eudiplo.Client.Tests;

public class EudiploApiClientCoreTests
{
    [Fact]
    public async Task GetVersionAsync_Success_ReturnsBody()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.OK, """{"version":"1.4.2"}""");

        var result = await client.GetVersionAsync();

        Assert.Contains("1.4.2", result);
        Assert.Equal("/version", handler.Requests[1].RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task GetVersionAsync_NonSuccessStatus_ReturnsNull()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.NotFound);

        Assert.Null(await client.GetVersionAsync());
    }

    [Fact]
    public async Task GetFrontendConfigAsync_Success_ReturnsBody()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.OK, """{"theme":"dark"}""");

        var result = await client.GetFrontendConfigAsync();

        Assert.Contains("dark", result);
        Assert.Equal("/frontend-config", handler.Requests[1].RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task GetFrontendConfigAsync_NonSuccessStatus_ReturnsNull()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.InternalServerError);

        Assert.Null(await client.GetFrontendConfigAsync());
    }
}
