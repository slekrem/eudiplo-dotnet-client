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
}
