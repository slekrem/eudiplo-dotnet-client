namespace Eudiplo.Client.Tests;

public class EudiploApiClientStatusListTests
{
    [Fact]
    public async Task GetStatusListConfigAsync_NotFound_ReturnsNull()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.NotFound);

        Assert.Null(await client.GetStatusListConfigAsync());
    }

    [Fact]
    public async Task SetStatusListConfigAsync_Failure_Throws()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.BadRequest, """{"message":"invalid"}""");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.SetStatusListConfigAsync("{}"));
    }

    [Fact]
    public async Task ResetStatusListConfigAsync_NotFound_DoesNotThrow()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.NotFound);

        var exception = await Record.ExceptionAsync(() => client.ResetStatusListConfigAsync());

        Assert.Null(exception);
    }

    [Fact]
    public async Task GetStatusListsAsync_ParsesList()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.OK, """[{"id":"sl-1"},{"id":"sl-2"}]""");

        var result = await client.GetStatusListsAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal("/api/status-lists", handler.Requests[1].RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task GetStatusListAsync_NotFound_ReturnsNull()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.NotFound);

        Assert.Null(await client.GetStatusListAsync("missing"));
    }

    [Fact]
    public async Task CreateStatusListAsync_Failure_Throws()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.BadRequest, """{"message":"invalid"}""");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.CreateStatusListAsync("{}"));
    }

    [Fact]
    public async Task UpdateStatusListAsync_Success_SendsPatch()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.OK, """{"id":"sl-1"}""");

        await client.UpdateStatusListAsync("sl-1", """{"size":1000}""");

        Assert.Equal(HttpMethod.Patch, handler.Requests[1].Method);
        Assert.Equal("/api/status-lists/sl-1", handler.Requests[1].RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task DeleteStatusListAsync_Failure_Throws()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.InternalServerError);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.DeleteStatusListAsync("sl-1"));
    }
}
