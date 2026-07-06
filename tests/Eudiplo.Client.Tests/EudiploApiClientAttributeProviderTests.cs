using System.Net;
using Eudiplo.Client.Tests.TestSupport;

namespace Eudiplo.Client.Tests;

public class EudiploApiClientAttributeProviderTests
{
    [Fact]
    public async Task GetAttributeProvidersAsync_ParsesList()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.OK, """[{"id":"ap-1"}]""");

        var result = await client.GetAttributeProvidersAsync();

        Assert.Single(result);
        Assert.Equal("/api/issuer/attribute-providers", handler.Requests[1].RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task GetAttributeProviderAsync_NotFound_ReturnsNull()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.NotFound);

        Assert.Null(await client.GetAttributeProviderAsync("missing"));
    }

    [Fact]
    public async Task CreateAttributeProviderAsync_Failure_Throws()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.BadRequest, """{"message":"invalid"}""");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.CreateAttributeProviderAsync("{}"));
    }

    [Fact]
    public async Task UpdateAttributeProviderAsync_Success_SendsPatch()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.OK, """{"id":"ap-1"}""");

        await client.UpdateAttributeProviderAsync("ap-1", """{"url":"https://x"}""");

        Assert.Equal(HttpMethod.Patch, handler.Requests[1].Method);
        Assert.Equal("/api/issuer/attribute-providers/ap-1", handler.Requests[1].RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task DeleteAttributeProviderAsync_Failure_Throws()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.InternalServerError);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.DeleteAttributeProviderAsync("ap-1"));
    }
}
