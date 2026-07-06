using System.Net;
using Eudiplo.Client.Tests.TestSupport;

namespace Eudiplo.Client.Tests;

public class EudiploApiClientKeyChainTests
{
    [Fact]
    public async Task GetAccessKeyChainsAsync_FiltersToUsageTypeAccessOnly()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.OK, """
            [
                {"id":"k1","usageType":"access","description":"Access key"},
                {"id":"k2","usageType":"attestation","description":"Attestation key"}
            ]
            """);

        var result = await client.GetAccessKeyChainsAsync();

        var entry = Assert.Single(result);
        Assert.Equal("k1", entry.Id);
        Assert.Equal("Access key", entry.Description);
    }

    [Fact]
    public async Task GetAccessKeyChainsAsync_MissingDescription_FallsBackToId()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.OK, """[{"id":"k1","usageType":"access"}]""");

        var result = await client.GetAccessKeyChainsAsync();

        Assert.Equal("k1", Assert.Single(result).Description);
    }

    [Fact]
    public async Task GetAccessKeyChainsAsync_NonSuccessStatus_ReturnsEmpty()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.InternalServerError);

        var result = await client.GetAccessKeyChainsAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task CreateKeyChainAsync_SendsExpectedBody_ReturnsRawResponse()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.OK, """{"id":"k1"}""");

        var result = await client.CreateKeyChainAsync("access", "ec", "my access key");

        Assert.Contains("\"id\":\"k1\"", result);
        Assert.Contains("\"usageType\":\"access\"", handler.Requests[1].Body);
        Assert.Contains("\"type\":\"ec\"", handler.Requests[1].Body);
        Assert.Contains("\"description\":\"my access key\"", handler.Requests[1].Body);
    }

    [Fact]
    public async Task CreateKeyChainAsync_Failure_Throws()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.BadRequest, """{"message":"bad type"}""");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.CreateKeyChainAsync("access", "invalid", null));
    }

    [Fact]
    public async Task ExportKeyChainAsync_Success_ReturnsBody()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.OK, """{"key":{"kty":"EC","d":"secret"}}""");

        var result = await client.ExportKeyChainAsync("k1");

        Assert.Contains("\"d\":\"secret\"", result);
        Assert.Equal("/api/key-chain/k1/export", handler.Requests[1].RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task ExportKeyChainAsync_NotFound_ReturnsNull()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.NotFound);

        var result = await client.ExportKeyChainAsync("missing");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetKeyChainsAsync_NoFilter_ReturnsAllEntries()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.OK, """
            [{"id":"k1","usageType":"access"},{"id":"k2","usageType":"attestation"}]
            """);

        var result = await client.GetKeyChainsAsync();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetKeyChainsAsync_WithUsageTypeFilter_ExcludesOthers()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.OK, """
            [{"id":"k1","usageType":"access"},{"id":"k2","usageType":"attestation"}]
            """);

        var result = await client.GetKeyChainsAsync(usageType: "attestation");

        var entry = Assert.Single(result);
        Assert.Equal("k2", entry.GetProperty("id").GetString());
    }

    [Fact]
    public async Task GetKeyChainAsync_Found_ReturnsElement()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.OK, """{"id":"k1","usageType":"access"}""");

        var result = await client.GetKeyChainAsync("k1");

        Assert.NotNull(result);
        Assert.Equal("/api/key-chain/k1", handler.Requests[1].RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task GetKeyChainAsync_NotFound_ReturnsNull()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.NotFound);

        Assert.Null(await client.GetKeyChainAsync("missing"));
    }

    [Fact]
    public async Task ImportKeyChainAsync_Success_SendsToImportPath()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.OK, """{"id":"k1"}""");

        var result = await client.ImportKeyChainAsync("""{"key":{"kty":"EC","d":"secret"}}""");

        Assert.Contains("k1", result);
        Assert.Equal("/api/key-chain/import", handler.Requests[1].RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task ImportKeyChainAsync_Failure_Throws()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.BadRequest, """{"message":"invalid key"}""");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.ImportKeyChainAsync("{}"));
    }

    [Fact]
    public async Task UpdateKeyChainAsync_Success_SendsPut()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.OK, """{"id":"k1"}""");

        await client.UpdateKeyChainAsync("k1", """{"description":"renamed"}""");

        Assert.Equal(HttpMethod.Put, handler.Requests[1].Method);
        Assert.Equal("/api/key-chain/k1", handler.Requests[1].RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task DeleteKeyChainAsync_Failure_Throws()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.Forbidden, """{"message":"in use"}""");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.DeleteKeyChainAsync("k1"));
    }

    [Fact]
    public async Task RotateKeyChainAsync_Success_ReturnsResponseBody()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.OK, """{"id":"k1","rotatedAt":"2026-01-01"}""");

        var result = await client.RotateKeyChainAsync("k1");

        Assert.Contains("rotatedAt", result);
        Assert.Equal("/api/key-chain/k1/rotate", handler.Requests[1].RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task GetKmsProvidersAsync_ParsesList()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.OK, """[{"name":"local"},{"name":"aws-kms"}]""");

        var result = await client.GetKmsProvidersAsync();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetKmsProviderHealthAsync_NonSuccessStatus_ReturnsNull()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.ServiceUnavailable);

        Assert.Null(await client.GetKmsProviderHealthAsync());
    }

    [Fact]
    public async Task GetKmsProviderConfigAsync_NotFound_ReturnsNull()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.NotFound);

        Assert.Null(await client.GetKmsProviderConfigAsync());
    }

    [Fact]
    public async Task SetKmsProviderConfigAsync_Failure_Throws()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.BadRequest, """{"message":"bad config"}""");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.SetKmsProviderConfigAsync("{}"));
    }

    [Fact]
    public async Task DeleteKmsProviderConfigAsync_NotFound_DoesNotThrow()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.NotFound);

        var exception = await Record.ExceptionAsync(() => client.DeleteKmsProviderConfigAsync());

        Assert.Null(exception);
    }

    [Fact]
    public async Task DeleteKmsProviderConfigAsync_OtherFailure_Throws()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.InternalServerError);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.DeleteKmsProviderConfigAsync());
    }
}
