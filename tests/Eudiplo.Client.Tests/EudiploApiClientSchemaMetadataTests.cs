using System.Net;
using Eudiplo.Client.Tests.TestSupport;

namespace Eudiplo.Client.Tests;

public class EudiploApiClientSchemaMetadataTests
{
    [Fact]
    public async Task SignSchemaMetadataAsync_Success_ReturnsResponseBody()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.OK, """{"id":"schema-1"}""");

        var result = await client.SignSchemaMetadataAsync("""{"vct":"my-credential"}""");

        Assert.Contains("schema-1", result);
        Assert.Equal(HttpMethod.Post, handler.Requests[1].Method);
        Assert.Equal("/api/schema-metadata/sign", handler.Requests[1].RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task SignSchemaMetadataAsync_Failure_ThrowsWithStatusAndBody()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.BadRequest, """{"message":"invalid schema"}""");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.SignSchemaMetadataAsync("{}"));
        Assert.Contains("400", ex.Message);
        Assert.Contains("invalid schema", ex.Message);
    }
}
