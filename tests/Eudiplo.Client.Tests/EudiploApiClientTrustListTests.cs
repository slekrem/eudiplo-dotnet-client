using System.Net;
using Eudiplo.Client.Tests.TestSupport;

namespace Eudiplo.Client.Tests;

public class EudiploApiClientTrustListTests
{
    [Fact]
    public async Task GetTrustListsAsync_ParsesEntries()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.OK, """
            [{"id":"tl-1","description":"My trust list","tenantId":"tenant-a"}]
            """);

        var result = await client.GetTrustListsAsync();

        var entry = Assert.Single(result);
        Assert.Equal("tl-1", entry.Id);
        Assert.Equal("My trust list", entry.Description);
        Assert.Equal("tenant-a", entry.TenantId);
    }

    [Fact]
    public async Task GetTrustListsAsync_MissingDescription_FallsBackToId()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.OK, """[{"id":"tl-1"}]""");

        var entry = Assert.Single(await client.GetTrustListsAsync());

        Assert.Equal("tl-1", entry.Description);
        Assert.Equal("", entry.TenantId);
    }

    [Fact]
    public async Task GetTrustListsAsync_EntryMissingId_IsSkipped()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.OK, """[{"description":"no id here"}]""");

        var result = await client.GetTrustListsAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetTrustListsAsync_NonSuccessStatus_ReturnsEmpty()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.InternalServerError);

        Assert.Empty(await client.GetTrustListsAsync());
    }

    [Fact]
    public async Task CreateTrustListAsync_Failure_Throws()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.BadRequest, """{"message":"bad shape"}""");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.CreateTrustListAsync("{}"));
    }

    [Fact]
    public async Task DeleteTrustListAsync_NotFound_DoesNotThrow()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.NotFound);

        var exception = await Record.ExceptionAsync(() => client.DeleteTrustListAsync("tl-1"));

        Assert.Null(exception);
    }

    [Fact]
    public async Task DeleteTrustListAsync_OtherFailure_Throws()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.InternalServerError);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.DeleteTrustListAsync("tl-1"));
    }

    [Fact]
    public async Task GetTrustListPublicJwkAsync_StripsPrivateKeyComponent()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.OK, """{"keyChainId":"kc-1"}""");
        handler.Enqueue(HttpStatusCode.OK, """
            {"key":{"kty":"EC","crv":"P-256","x":"xval","y":"yval","d":"PRIVATE","kid":"k1","alg":"ES256"}}
            """);

        var jwk = await client.GetTrustListPublicJwkAsync("tl-1");

        Assert.NotNull(jwk);
        Assert.DoesNotContain("PRIVATE", jwk);
        Assert.DoesNotContain("\"d\"", jwk);
        Assert.Contains("\"use\":\"sig\"", jwk);
        Assert.Contains("\"kty\":\"EC\"", jwk);
    }

    [Fact]
    public async Task GetTrustListPublicJwkAsync_TrustListNotFound_ReturnsNull()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.NotFound);

        var result = await client.GetTrustListPublicJwkAsync("missing");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetTrustListPublicJwkAsync_TrustListMissingKeyChainId_ReturnsNull()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.OK, """{"id":"tl-1"}""");

        var result = await client.GetTrustListPublicJwkAsync("tl-1");

        Assert.Null(result);
        // Only one request beyond the token exchange — no export call was attempted.
        Assert.Equal(2, handler.Requests.Count);
    }

    [Fact]
    public async Task GetTrustListPublicJwkAsync_KeyChainExportFails_ReturnsNull()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.OK, """{"keyChainId":"kc-1"}""");
        handler.Enqueue(HttpStatusCode.NotFound);

        var result = await client.GetTrustListPublicJwkAsync("tl-1");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetTrustListJsonAsync_ReturnsRawBody()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.OK, """{"id":"tl-1","description":"x"}""");

        var result = await client.GetTrustListJsonAsync("tl-1");

        Assert.Contains("tl-1", result);
    }

    [Fact]
    public async Task UpdateTrustListAsync_Failure_Throws()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.BadRequest, """{"message":"invalid"}""");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.UpdateTrustListAsync("tl-1", "{}"));
    }

    [Fact]
    public async Task UpdateTrustListAsync_Success_SendsPut()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.OK);

        await client.UpdateTrustListAsync("tl-1", """{"id":"tl-1"}""");

        Assert.Equal(HttpMethod.Put, handler.Requests[1].Method);
        Assert.Equal("/api/trust-list/tl-1", handler.Requests[1].RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task ExportTrustListAsync_Success_ReturnsBody()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.OK, """{"id":"tl-1","entries":[]}""");

        var result = await client.ExportTrustListAsync("tl-1");

        Assert.Contains("entries", result);
        Assert.Equal("/api/trust-list/tl-1/export", handler.Requests[1].RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task ExportTrustListAsync_NotFound_ReturnsNull()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.NotFound);

        Assert.Null(await client.ExportTrustListAsync("missing"));
    }

    [Fact]
    public async Task GetTrustListVersionsAsync_ParsesList()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.OK, """[{"versionId":"v1"},{"versionId":"v2"}]""");

        var result = await client.GetTrustListVersionsAsync("tl-1");

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetTrustListVersionAsync_Found_ReturnsElement()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.OK, """{"versionId":"v1"}""");

        var result = await client.GetTrustListVersionAsync("tl-1", "v1");

        Assert.NotNull(result);
        Assert.Equal("/api/trust-list/tl-1/versions/v1", handler.Requests[1].RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task GetTrustListVersionAsync_NotFound_ReturnsNull()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.NotFound);

        Assert.Null(await client.GetTrustListVersionAsync("tl-1", "missing"));
    }
}
