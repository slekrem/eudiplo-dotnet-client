namespace Eudiplo.Client.Tests;

public class EudiploApiClientRegistrarTests
{
    [Fact]
    public async Task GetRegistrarConfigAsync_NotFound_ReturnsNull()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.NotFound);

        var result = await client.GetRegistrarConfigAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task GetRegistrarConfigAsync_Success_ReturnsRawBody()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.OK, """{"hasPassword":true,"clientSecret":"plaintext"}""");

        var result = await client.GetRegistrarConfigAsync();

        Assert.Contains("plaintext", result);
    }

    [Fact]
    public async Task CreateRegistrarConfigAsync_Failure_ThrowsWithStatusAndBody()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.BadRequest, """{"message":"invalid credentials"}""");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.CreateRegistrarConfigAsync("{}"));
        Assert.Contains("400", ex.Message);
        Assert.Contains("invalid credentials", ex.Message);
    }

    [Fact]
    public async Task CreateRegistrarConfigAsync_Success_ReturnsBody()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.OK, """{"id":"cfg-1"}""");

        var result = await client.CreateRegistrarConfigAsync("""{"username":"u"}""");

        Assert.Contains("cfg-1", result);
    }

    [Fact]
    public async Task UpdateRegistrarConfigAsync_Success_SendsPatch()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.OK, """{"id":"cfg-1"}""");

        await client.UpdateRegistrarConfigAsync("""{"username":"u2"}""");

        Assert.Equal(HttpMethod.Patch, handler.Requests[1].Method);
        Assert.Equal("/api/registrar/config", handler.Requests[1].RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task DeleteRegistrarConfigAsync_NotFound_DoesNotThrow()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.NotFound);

        var exception = await Record.ExceptionAsync(() => client.DeleteRegistrarConfigAsync());

        Assert.Null(exception);
    }

    [Fact]
    public async Task DeleteRegistrarConfigAsync_OtherFailure_Throws()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.InternalServerError);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.DeleteRegistrarConfigAsync());
    }

    [Fact]
    public async Task CreateAccessCertificateViaRegistrarAsync_SendsKeyIdField_ReturnsIdAndPem()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.OK, """{"id":"cert-1","crt":"-----BEGIN CERTIFICATE-----..."}""");

        var (id, pem) = await client.CreateAccessCertificateViaRegistrarAsync("kc-1");

        Assert.Equal("cert-1", id);
        Assert.Contains("BEGIN CERTIFICATE", pem);
        // Field name must be "keyId", not "keyChainId" — EUDIPLO's schema (not its prose docs)
        // is the reliable source for this field name.
        Assert.Contains("\"keyId\":\"kc-1\"", handler.Requests[1].Body);
    }

    [Fact]
    public async Task CreateAccessCertificateViaRegistrarAsync_ResponseMissingFields_Throws()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.OK, """{"id":"cert-1"}""");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.CreateAccessCertificateViaRegistrarAsync("kc-1"));
    }
}
