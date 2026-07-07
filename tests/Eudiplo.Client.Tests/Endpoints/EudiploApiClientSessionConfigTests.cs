namespace Eudiplo.Client.Tests;

public class EudiploApiClientSessionConfigTests
{
    [Fact]
    public async Task GetSessionConfigAsync_NotFound_ReturnsNull()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.NotFound);

        Assert.Null(await client.GetSessionConfigAsync());
    }

    [Fact]
    public async Task SetSessionConfigAsync_Success_SendsPut()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.OK, """{"ttlSeconds":600}""");

        var result = await client.SetSessionConfigAsync("""{"ttlSeconds":600}""");

        Assert.Contains("600", result);
        Assert.Equal(HttpMethod.Put, handler.Requests[1].Method);
        Assert.Equal("/api/session-config", handler.Requests[1].RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task SetSessionConfigAsync_Failure_Throws()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.BadRequest, """{"message":"invalid"}""");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.SetSessionConfigAsync("{}"));
    }

    [Fact]
    public async Task ResetSessionConfigAsync_NotFound_DoesNotThrow()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.NotFound);

        var exception = await Record.ExceptionAsync(() => client.ResetSessionConfigAsync());

        Assert.Null(exception);
    }

    [Fact]
    public async Task ResetSessionConfigAsync_OtherFailure_Throws()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.InternalServerError);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.ResetSessionConfigAsync());
    }
}
