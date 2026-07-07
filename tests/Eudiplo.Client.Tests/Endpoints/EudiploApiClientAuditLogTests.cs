namespace Eudiplo.Client.Tests;

public class EudiploApiClientAuditLogTests
{
    [Fact]
    public async Task GetAuditLogAsync_ParsesList()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.OK, """[{"action":"client.create"},{"action":"tenant.delete"}]""");

        var result = await client.GetAuditLogAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal("/api/admin/audit-logs", handler.Requests[1].RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task GetAuditLogAsync_NonSuccessStatus_ReturnsEmpty()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.Forbidden);

        Assert.Empty(await client.GetAuditLogAsync());
    }
}
