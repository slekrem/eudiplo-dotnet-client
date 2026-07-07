namespace Eudiplo.Client.Tests;

public class EudiploApiClientUserTests
{
    [Fact]
    public async Task GetUsersAsync_ParsesList()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.OK, """[{"id":"u1"},{"id":"u2"}]""");

        var result = await client.GetUsersAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal("/api/user", handler.Requests[1].RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task GetUsersAsync_NonSuccessStatus_ReturnsEmpty()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.InternalServerError);

        Assert.Empty(await client.GetUsersAsync());
    }

    [Fact]
    public async Task GetUserAsync_NotFound_ReturnsNull()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.NotFound);

        Assert.Null(await client.GetUserAsync("missing"));
    }

    [Fact]
    public async Task CreateUserAsync_Success_ReturnsBody()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.OK, """{"id":"u1"}""");

        var result = await client.CreateUserAsync("""{"email":"a@example.com"}""");

        Assert.Contains("u1", result);
        Assert.Equal("/api/user", handler.Requests[1].RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task CreateUserAsync_Failure_Throws()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.Conflict, """{"message":"already exists"}""");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.CreateUserAsync("{}"));
    }

    [Fact]
    public async Task UpdateUserAsync_Success_SendsPatch()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.OK, """{"id":"u1"}""");

        await client.UpdateUserAsync("u1", """{"email":"b@example.com"}""");

        Assert.Equal(HttpMethod.Patch, handler.Requests[1].Method);
        Assert.Equal("/api/user/u1", handler.Requests[1].RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task DeleteUserAsync_Failure_Throws()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.Forbidden, """{"message":"forbidden"}""");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.DeleteUserAsync("u1"));
    }
}
