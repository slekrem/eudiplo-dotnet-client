using System.Net;
using Eudiplo.Client.Tests.TestSupport;

namespace Eudiplo.Client.Tests;

public class EudiploApiClientWebhookEndpointTests
{
    [Fact]
    public async Task GetWebhookEndpointsAsync_ParsesList()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.OK, """[{"id":"wh-1"}]""");

        var result = await client.GetWebhookEndpointsAsync();

        Assert.Single(result);
        Assert.Equal("/api/issuer/webhook-endpoints", handler.Requests[1].RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task GetWebhookEndpointAsync_NotFound_ReturnsNull()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.NotFound);

        Assert.Null(await client.GetWebhookEndpointAsync("missing"));
    }

    [Fact]
    public async Task CreateWebhookEndpointAsync_Failure_Throws()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.BadRequest, """{"message":"invalid url"}""");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.CreateWebhookEndpointAsync("{}"));
    }

    [Fact]
    public async Task UpdateWebhookEndpointAsync_Success_SendsPatch()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.OK, """{"id":"wh-1"}""");

        await client.UpdateWebhookEndpointAsync("wh-1", """{"url":"https://x"}""");

        Assert.Equal(HttpMethod.Patch, handler.Requests[1].Method);
        Assert.Equal("/api/issuer/webhook-endpoints/wh-1", handler.Requests[1].RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task DeleteWebhookEndpointAsync_Failure_Throws()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.InternalServerError);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.DeleteWebhookEndpointAsync("wh-1"));
    }
}
