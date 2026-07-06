using System.Net;
using Eudiplo.Client.Tests.TestSupport;

namespace Eudiplo.Client.Tests;

public class EudiploApiClientSessionTests
{
    [Fact]
    public async Task GetSessionsAsync_NoQuery_HitsPlainPath()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.OK, """[{"id":"s1"},{"id":"s2"}]""");

        var result = await client.GetSessionsAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal("/api/session", handler.Requests[1].RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task GetSessionsAsync_WithQueryWithoutLeadingQuestionMark_IsPrefixedCorrectly()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.OK, """[]""");

        await client.GetSessionsAsync("page=2&limit=50");

        Assert.Equal("/api/session", handler.Requests[1].RequestUri!.AbsolutePath);
        Assert.Equal("?page=2&limit=50", handler.Requests[1].RequestUri!.Query);
    }

    [Fact]
    public async Task GetSessionsAsync_WithQueryIncludingLeadingQuestionMark_IsNotDoubled()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.OK, """[]""");

        await client.GetSessionsAsync("?page=1");

        Assert.Equal("?page=1", handler.Requests[1].RequestUri!.Query);
    }

    [Fact]
    public async Task GetSessionsAsync_NonSuccessStatus_ReturnsEmpty()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.InternalServerError);

        Assert.Empty(await client.GetSessionsAsync());
    }

    [Fact]
    public async Task DeleteSessionAsync_Failure_Throws()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.NotFound, """{"message":"not found"}""");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.DeleteSessionAsync("s1"));
    }

    [Fact]
    public async Task DeleteSessionAsync_Success_DoesNotThrow()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.OK);

        var exception = await Record.ExceptionAsync(() => client.DeleteSessionAsync("s1"));

        Assert.Null(exception);
    }

    [Fact]
    public async Task GetSessionLogsAsync_ParsesList()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.OK, """[{"message":"started"},{"message":"completed"}]""");

        var result = await client.GetSessionLogsAsync("s1");

        Assert.Equal(2, result.Count);
        Assert.Equal("/api/session/s1/logs", handler.Requests[1].RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task GetSessionLogsAsync_NonSuccessStatus_ReturnsEmpty()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.NotFound);

        Assert.Empty(await client.GetSessionLogsAsync("missing"));
    }

    [Fact]
    public async Task SubscribeToSessionEventsAsync_YieldsEachDataLine()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.OK, "data: {\"status\":\"pending\"}\n\ndata: {\"status\":\"issued\"}\n\n");

        var events = new List<string>();
        await foreach (var e in client.SubscribeToSessionEventsAsync("s1"))
            events.Add(e);

        Assert.Equal(["{\"status\":\"pending\"}", "{\"status\":\"issued\"}"], events);
        Assert.Equal("/api/session/s1/events", handler.Requests[1].RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task SubscribeToSessionEventsAsync_IgnoresNonDataLines()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.OK, "event: ping\ndata: hello\n\n");

        var events = new List<string>();
        await foreach (var e in client.SubscribeToSessionEventsAsync("s1"))
            events.Add(e);

        Assert.Equal(["hello"], events);
    }

    [Fact]
    public async Task SubscribeToSessionEventsAsync_NonSuccessStatus_Throws()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.NotFound);

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await foreach (var _ in client.SubscribeToSessionEventsAsync("missing")) { }
        });
    }
}
