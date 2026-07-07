namespace Eudiplo.Client.Tests;

public class EudiploApiClientDeferredTests
{
    [Fact]
    public async Task CompleteDeferredAsync_Success_ReturnsBody()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.OK, """{"status":"completed"}""");

        var result = await client.CompleteDeferredAsync("txn-1", """{"claims":{"name":"Max"}}""");

        Assert.Contains("completed", result);
        Assert.Equal("/api/issuer/deferred/txn-1/complete", handler.Requests[1].RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task CompleteDeferredAsync_Failure_Throws()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.NotFound, """{"message":"unknown transaction"}""");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.CompleteDeferredAsync("missing", "{}"));
    }

    [Fact]
    public async Task FailDeferredAsync_WithoutBody_Succeeds()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.OK);

        var exception = await Record.ExceptionAsync(() => client.FailDeferredAsync("txn-1"));

        Assert.Null(exception);
        Assert.Null(handler.Requests[1].Body);
        Assert.Equal("/api/issuer/deferred/txn-1/fail", handler.Requests[1].RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task FailDeferredAsync_WithBody_SendsIt()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.OK);

        await client.FailDeferredAsync("txn-1", """{"reason":"timeout"}""");

        Assert.Contains("timeout", handler.Requests[1].Body);
    }

    [Fact]
    public async Task FailDeferredAsync_Failure_Throws()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.NotFound, """{"message":"unknown"}""");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.FailDeferredAsync("missing"));
    }
}
