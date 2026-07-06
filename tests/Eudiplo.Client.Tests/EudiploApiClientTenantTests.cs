using System.Net;
using Eudiplo.Client.Tests.TestSupport;

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
}
