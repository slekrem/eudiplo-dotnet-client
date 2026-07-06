using System.Net;
using Eudiplo.Client.Tests.TestSupport;

namespace Eudiplo.Client.Tests;

public class EudiploApiClientClientTests
{
    [Fact]
    public async Task GetClientsAsync_ParsesPlainJsonArray()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.OK, """[{"id":"c1"},{"id":"c2"}]""");

        var clients = await client.GetClientsAsync();

        Assert.Equal(2, clients.Count);
        Assert.Equal("/api/client", handler.Requests[1].RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task GetClientsAsync_ParsesItemsWrapperShape()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.OK, """{"items":[{"id":"c1"}]}""");

        var clients = await client.GetClientsAsync();

        Assert.Single(clients);
    }

    [Fact]
    public async Task GetClientsAsync_NonSuccessStatus_ReturnsEmpty()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.InternalServerError);

        var clients = await client.GetClientsAsync();

        Assert.Empty(clients);
    }

    [Fact]
    public async Task GetClientAsync_Found_ReturnsElement()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.OK, """{"id":"c1","description":"test"}""");

        var result = await client.GetClientAsync("c1");

        Assert.NotNull(result);
        Assert.Equal("c1", result.Value.GetProperty("id").GetString());
        Assert.Equal("/api/client/c1", handler.Requests[1].RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task GetClientAsync_NotFound_ReturnsNull()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.NotFound);

        var result = await client.GetClientAsync("missing");

        Assert.Null(result);
    }

    [Fact]
    public async Task CreateClientAsync_Success_ReturnsRawResponseBody()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.OK, """{"id":"c1","clientSecret":"s3cr3t"}""");

        var result = await client.CreateClientAsync("""{"description":"my client"}""");

        Assert.Contains("s3cr3t", result);
        Assert.Equal(HttpMethod.Post, handler.Requests[1].Method);
        Assert.Equal("/api/client", handler.Requests[1].RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task CreateClientAsync_Failure_ThrowsWithStatusAndBody()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.BadRequest, """{"message":"invalid roles"}""");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.CreateClientAsync("{}"));
        Assert.Contains("400", ex.Message);
        Assert.Contains("invalid roles", ex.Message);
    }

    [Fact]
    public async Task UpdateClientAsync_Failure_Throws()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.NotFound, """{"message":"not found"}""");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.UpdateClientAsync("c1", "{}"));
    }

    [Fact]
    public async Task UpdateClientAsync_Success_SendsPatchToExpectedPath()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.OK);

        await client.UpdateClientAsync("c1", """{"description":"renamed"}""");

        Assert.Equal(HttpMethod.Patch, handler.Requests[1].Method);
        Assert.Equal("/api/client/c1", handler.Requests[1].RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task DeleteClientAsync_Failure_Throws()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.Forbidden, """{"message":"forbidden"}""");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.DeleteClientAsync("c1"));
    }

    [Fact]
    public async Task RotateClientSecretAsync_ReturnsNewSecret()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.OK, """{"secret":"new-secret-value"}""");

        var secret = await client.RotateClientSecretAsync("c1");

        Assert.Equal("new-secret-value", secret);
        Assert.Equal("/api/client/c1/rotate-secret", handler.Requests[1].RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task RotateClientSecretAsync_ResponseMissingSecretField_ThrowsKeyNotFound()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.OK, "{}");

        // GetProperty("secret") throws KeyNotFoundException when the field is absent entirely —
        // the "?? throw InvalidOperationException" guard only covers the field being present but null.
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => client.RotateClientSecretAsync("c1"));
    }
}
