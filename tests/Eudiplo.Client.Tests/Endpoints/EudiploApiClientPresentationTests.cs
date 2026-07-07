namespace Eudiplo.Client.Tests;

public class EudiploApiClientPresentationTests
{
    [Fact]
    public async Task CreateOfferAsync_ReturnsRequestUrlAndSessionId()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.OK, """{"uri":"openid4vp://request?x=1","session":"sess-123"}""");

        var (requestUrl, sessionId) = await client.CreateOfferAsync("ticket-checkin");

        Assert.Equal("openid4vp://request?x=1", requestUrl);
        Assert.Equal("sess-123", sessionId);
        Assert.Equal(HttpMethod.Post, handler.Requests[1].Method);
        Assert.Equal("/api/verifier/offer", handler.Requests[1].RequestUri!.AbsolutePath);
        Assert.Contains("\"requestId\":\"ticket-checkin\"", handler.Requests[1].Body);
        Assert.DoesNotContain("redirectUri", handler.Requests[1].Body);
    }

    [Fact]
    public async Task CreateOfferAsync_WithRedirectUri_IncludesItInRequestBody()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.OK, """{"uri":"openid4vp://x","session":"s1"}""");

        await client.CreateOfferAsync("cfg-1", redirectUri: "https://issuer.example/callback");

        Assert.Contains("\"redirectUri\":\"https://issuer.example/callback\"", handler.Requests[1].Body);
    }

    [Fact]
    public async Task CreateOfferAsync_ResponseMissingUriOrSession_Throws()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.OK, """{"uri":"openid4vp://x"}""");

        await Assert.ThrowsAsync<InvalidOperationException>(() => client.CreateOfferAsync("cfg-1"));
    }

    [Fact]
    public async Task CreateOfferAsync_NonSuccessStatus_Throws()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.InternalServerError);

        await Assert.ThrowsAsync<HttpRequestException>(() => client.CreateOfferAsync("cfg-1"));
    }

    [Fact]
    public async Task GetSessionAsync_NonSuccessStatus_ReturnsNullInsteadOfThrowing()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.NotFound);

        var result = await client.GetSessionAsync("missing-session");

        Assert.Null(result);
    }
}
