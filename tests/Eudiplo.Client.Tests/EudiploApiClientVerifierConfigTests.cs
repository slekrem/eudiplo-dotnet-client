using System.Net;
using Eudiplo.Client.Tests.TestSupport;

namespace Eudiplo.Client.Tests;

public class EudiploApiClientVerifierConfigTests
{
    [Fact]
    public async Task GetVerifierConfigJsonAsync_NotFound_ReturnsNull()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.NotFound);

        var result = await client.GetVerifierConfigJsonAsync("cfg-1");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetVerifierConfigJsonAsync_Success_ReturnsBody()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.OK, """{"id":"cfg-1"}""");

        var result = await client.GetVerifierConfigJsonAsync("cfg-1");

        Assert.Contains("cfg-1", result);
    }

    [Fact]
    public async Task PostVerifierConfigAsync_Failure_ThrowsWithStatusAndBody()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.BadRequest, """{"message":"invalid dcql"}""");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.PostVerifierConfigAsync("{}"));
        Assert.Contains("400", ex.Message);
        Assert.Contains("invalid dcql", ex.Message);
    }

    [Fact]
    public async Task UpdateVerifierConfigAsync_Success_SendsPatch()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.OK);

        await client.UpdateVerifierConfigAsync("cfg-1", """{"dcql":{}}""");

        Assert.Equal(HttpMethod.Patch, handler.Requests[1].Method);
        Assert.Equal("/api/verifier/config/cfg-1", handler.Requests[1].RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task DeleteVerifierConfigAsync_NotFound_DoesNotThrow()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.NotFound);

        var exception = await Record.ExceptionAsync(() => client.DeleteVerifierConfigAsync("cfg-1"));

        Assert.Null(exception);
    }

    [Fact]
    public async Task DeleteVerifierConfigAsync_OtherFailure_Throws()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.InternalServerError);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.DeleteVerifierConfigAsync("cfg-1"));
    }

    [Fact]
    public async Task GetVerifierConfigsAsync_ParsesList()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.OK, """[{"id":"cfg-1"},{"id":"cfg-2"}]""");

        var result = await client.GetVerifierConfigsAsync();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetVerifierConfigsAsync_NonSuccessStatus_ReturnsEmpty()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.InternalServerError);

        Assert.Empty(await client.GetVerifierConfigsAsync());
    }

    [Fact]
    public async Task ReissueRegistrationCertificateAsync_Success_SendsExpectedPath()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.OK);

        await client.ReissueRegistrationCertificateAsync("cfg-1");

        Assert.Equal("/api/verifier/config/cfg-1/registration-cert/reissue", handler.Requests[1].RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task ReissueRegistrationCertificateAsync_Failure_Throws()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.InternalServerError, """{"message":"registrar unreachable"}""");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.ReissueRegistrationCertificateAsync("cfg-1"));
    }

    [Fact]
    public async Task ResolveIssuerMetadataAsync_Success_ReturnsBody()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.OK, """{"issuer":"https://issuer.example"}""");

        var result = await client.ResolveIssuerMetadataAsync("""{"url":"https://issuer.example"}""");

        Assert.Contains("issuer.example", result);
        Assert.Equal("/api/verifier/config/issuer-metadata/resolve", handler.Requests[1].RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task ResolveIssuerMetadataAsync_Failure_Throws()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.BadRequest, """{"message":"unreachable"}""");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.ResolveIssuerMetadataAsync("{}"));
    }

    [Fact]
    public async Task ResolveSchemaMetadataAsync_Success_ReturnsBody()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.OK, """{"vct":"my-credential"}""");

        var result = await client.ResolveSchemaMetadataAsync("""{"id":"schema-1"}""");

        Assert.Contains("my-credential", result);
        Assert.Equal("/api/verifier/config/schema-metadata/resolve", handler.Requests[1].RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task ResolveSchemaMetadataAsync_Failure_Throws()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.NotFound, """{"message":"not found"}""");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.ResolveSchemaMetadataAsync("{}"));
    }

    [Fact]
    public async Task GetSchemaMetadataCatalogAsync_ParsesList()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.OK, """[{"id":"s1"},{"id":"s2"}]""");

        var result = await client.GetSchemaMetadataCatalogAsync();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetSchemaMetadataCatalogAsync_NonSuccessStatus_ReturnsEmpty()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.InternalServerError);

        Assert.Empty(await client.GetSchemaMetadataCatalogAsync());
    }
}
