namespace Eudiplo.Client.Tests;

public class EudiploApiClientAuthTests
{
    [Fact]
    public async Task GetSessionAsync_FetchesTokenFirst_ThenSendsAuthorizedRequest()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken("tok-1");
        handler.Enqueue(HttpStatusCode.OK, """{"status":"pending"}""");

        var result = await client.GetSessionAsync("session-1");

        Assert.Equal("""{"status":"pending"}""", result);
        Assert.Equal(2, handler.Requests.Count);
        Assert.Equal(HttpMethod.Post, handler.Requests[0].Method);
        Assert.Equal("/api/oauth2/token", handler.Requests[0].RequestUri!.AbsolutePath);
        Assert.Equal(HttpMethod.Get, handler.Requests[1].Method);
        Assert.Equal("/api/session/session-1", handler.Requests[1].RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task TokenRequest_SendsClientCredentialsGrantWithConfiguredIdAndSecret()
    {
        var (client, handler) = TestClientFactory.Create(clientId: "my-id", clientSecret: "my-secret");
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.OK, """{"status":"ok"}""");

        await client.GetSessionAsync("s1");

        var tokenBody = handler.Requests[0].Body;
        Assert.Contains("\"grant_type\":\"client_credentials\"", tokenBody);
        Assert.Contains("\"client_id\":\"my-id\"", tokenBody);
        Assert.Contains("\"client_secret\":\"my-secret\"", tokenBody);
    }

    [Fact]
    public async Task Token_IsCached_AcrossMultipleCalls()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.OK, """{"status":"a"}""");
        handler.Enqueue(HttpStatusCode.OK, """{"status":"b"}""");

        await client.GetSessionAsync("s1");
        await client.GetSessionAsync("s2");

        var tokenRequests = handler.Requests.Count(r => r.RequestUri!.AbsolutePath == "/api/oauth2/token");
        Assert.Equal(1, tokenRequests);
        Assert.Equal(3, handler.Requests.Count);
    }

    [Fact]
    public async Task On401_TokenIsInvalidatedAndRequestIsRetriedOnce()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken("tok-1");
        handler.Enqueue(HttpStatusCode.Unauthorized);
        handler.EnqueueToken("tok-2");
        handler.Enqueue(HttpStatusCode.OK, """{"status":"ok"}""");

        var result = await client.GetSessionAsync("s1");

        Assert.Equal("""{"status":"ok"}""", result);
        Assert.Equal(4, handler.Requests.Count);
        var tokenRequests = handler.Requests.Count(r => r.RequestUri!.AbsolutePath == "/api/oauth2/token");
        Assert.Equal(2, tokenRequests);
    }

    [Fact]
    public async Task On401_RetriedRequestStillUnauthorized_DoesNotLoopForever()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken("tok-1");
        handler.Enqueue(HttpStatusCode.Unauthorized);
        handler.EnqueueToken("tok-2");
        handler.Enqueue(HttpStatusCode.Unauthorized);

        // GetSessionAsync treats any non-success status as "not found" (returns null) —
        // this asserts the retry-once behavior terminates rather than retrying indefinitely.
        var result = await client.GetSessionAsync("s1");

        Assert.Null(result);
        Assert.Equal(4, handler.Requests.Count);
    }

    [Fact]
    public async Task SecondCallAfter401Retry_UsesTheNewCachedToken()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken("tok-1");
        handler.Enqueue(HttpStatusCode.Unauthorized);
        handler.EnqueueToken("tok-2");
        handler.Enqueue(HttpStatusCode.OK, """{"status":"ok-1"}""");
        handler.Enqueue(HttpStatusCode.OK, """{"status":"ok-2"}""");

        await client.GetSessionAsync("s1");
        await client.GetSessionAsync("s2");

        // 2 token fetches total (initial + one after 401), not 3 — the refreshed token
        // from the retry is cached and reused for the second call.
        var tokenRequests = handler.Requests.Count(r => r.RequestUri!.AbsolutePath == "/api/oauth2/token");
        Assert.Equal(2, tokenRequests);
        Assert.Equal(5, handler.Requests.Count);
    }
}
