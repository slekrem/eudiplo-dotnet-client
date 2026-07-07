namespace Eudiplo.Client.Tests;

public class EudiploApiClientTenantTests
{
    [Fact]
    public async Task GetTenantsAsync_ParsesPlainJsonArray()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.OK, """[{"id":"t1"},{"id":"t2"}]""");

        var tenants = await client.GetTenantsAsync();

        Assert.Equal(2, tenants.Count);
    }

    [Fact]
    public async Task GetTenantsAsync_ParsesItemsWrapperShape()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.OK, """{"items":[{"id":"t1"}]}""");

        var tenants = await client.GetTenantsAsync();

        Assert.Single(tenants);
    }

    [Fact]
    public async Task GetTenantsAsync_NonSuccessStatus_ReturnsEmptyListInsteadOfThrowing()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.InternalServerError);

        var tenants = await client.GetTenantsAsync();

        Assert.Empty(tenants);
    }

    [Fact]
    public async Task CreateTenantAsync_Failure_ThrowsWithStatusAndBody()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.Conflict, """{"message":"already exists"}""");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.CreateTenantAsync("""{"id":"dup"}"""));

        Assert.Contains("409", ex.Message);
        Assert.Contains("already exists", ex.Message);
    }

    [Fact]
    public async Task DeleteTenantAsync_Success_DoesNotThrow()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.OK);

        var exception = await Record.ExceptionAsync(() => client.DeleteTenantAsync("t1"));

        Assert.Null(exception);
    }

    [Fact]
    public async Task GetTenantAsync_Found_ReturnsElement()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.OK, """{"id":"t1","name":"Tenant One"}""");

        var result = await client.GetTenantAsync("t1");

        Assert.NotNull(result);
        Assert.Equal("t1", result.Value.GetProperty("id").GetString());
        Assert.Equal("/api/tenant/t1", handler.Requests[1].RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task GetTenantAsync_NotFound_ReturnsNull()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.NotFound);

        var result = await client.GetTenantAsync("missing");

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateTenantAsync_Success_SendsPatchToExpectedPath()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.OK, """{"id":"t1","name":"Renamed"}""");

        var result = await client.UpdateTenantAsync("t1", """{"name":"Renamed"}""");

        Assert.Contains("Renamed", result);
        Assert.Equal(HttpMethod.Patch, handler.Requests[1].Method);
        Assert.Equal("/api/tenant/t1", handler.Requests[1].RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task UpdateTenantAsync_Failure_Throws()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.NotFound, """{"message":"not found"}""");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.UpdateTenantAsync("missing", "{}"));
    }
}
