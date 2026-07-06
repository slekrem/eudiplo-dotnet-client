using System.Net;
using Eudiplo.Client.Tests.TestSupport;

namespace Eudiplo.Client.Tests;

public class EudiploApiClientIssuerTests
{
    [Fact]
    public async Task UpsertCredentialConfigAsync_DeletesThenPosts_InOrder()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.NotFound); // delete: didn't exist yet — ignored
        handler.Enqueue(HttpStatusCode.OK, "{}");  // post: created

        await client.UpsertCredentialConfigAsync("cfg-1", """{"id":"cfg-1"}""");

        Assert.Equal(HttpMethod.Delete, handler.Requests[1].Method);
        Assert.Equal("/api/issuer/credentials/cfg-1", handler.Requests[1].RequestUri!.AbsolutePath);
        Assert.Equal(HttpMethod.Post, handler.Requests[2].Method);
        Assert.Equal("/api/issuer/credentials", handler.Requests[2].RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task UpsertCredentialConfigAsync_PostFailure_ThrowsWithStatusAndBody()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.NotFound);
        handler.Enqueue(HttpStatusCode.BadRequest, """{"message":"invalid"}""");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.UpsertCredentialConfigAsync("cfg-1", "{}"));

        Assert.Contains("400", ex.Message);
        Assert.Contains("invalid", ex.Message);
    }

    [Fact]
    public async Task CreateIssuanceOfferAsync_ReturnsUriAndSessionId()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.OK, """{"uri":"openid-credential-offer://x","session":"s-99"}""");

        var (uri, sessionId) = await client.CreateIssuanceOfferAsync("{}");

        Assert.Equal("openid-credential-offer://x", uri);
        Assert.Equal("s-99", sessionId);
    }

    [Fact]
    public async Task RevokeSessionAsync_SendsExpectedBodyShape()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.OK);

        await client.RevokeSessionAsync("s-1", "cfg-1", 1);

        var body = handler.Requests[1].Body!;
        Assert.Contains("\"sessionId\":\"s-1\"", body);
        Assert.Contains("\"credentialConfigurationId\":\"cfg-1\"", body);
        Assert.Contains("\"status\":1", body);
    }

    [Fact]
    public async Task GetCredentialConfigsAsync_ParsesList()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.OK, """[{"id":"cfg-1"},{"id":"cfg-2"}]""");

        var result = await client.GetCredentialConfigsAsync();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetCredentialConfigAsync_NotFound_ReturnsNull()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.NotFound);

        Assert.Null(await client.GetCredentialConfigAsync("missing"));
    }

    [Fact]
    public async Task PatchCredentialConfigAsync_Success_SendsPatch()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.OK, """{"id":"cfg-1"}""");

        await client.PatchCredentialConfigAsync("cfg-1", """{"display":{}}""");

        Assert.Equal(HttpMethod.Patch, handler.Requests[1].Method);
        Assert.Equal("/api/issuer/credentials/cfg-1", handler.Requests[1].RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task PatchCredentialConfigAsync_Failure_Throws()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.BadRequest, """{"message":"invalid"}""");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.PatchCredentialConfigAsync("cfg-1", "{}"));
    }

    [Fact]
    public async Task GetIssuerConfigJsonAsync_NonSuccessStatus_ReturnsNull()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.NotFound);

        var result = await client.GetIssuerConfigJsonAsync();

        Assert.Null(result);
    }
}
