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

    [Fact]
    public async Task SignSchemaMetadataVersionAsync_Success_ReturnsBody()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.OK, """{"version":2}""");

        var result = await client.SignSchemaMetadataVersionAsync("""{"id":"schema-1"}""");

        Assert.Contains("2", result);
        Assert.Equal("/api/schema-metadata/sign-version", handler.Requests[1].RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task SignSchemaMetadataVersionAsync_Failure_Throws()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.BadRequest, """{"message":"invalid"}""");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.SignSchemaMetadataVersionAsync("{}"));
    }

    [Fact]
    public async Task GetSchemaMetadataVocabulariesAsync_ParsesList()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.OK, """[{"name":"eu.europa.ec.eudi"}]""");

        var result = await client.GetSchemaMetadataVocabulariesAsync();

        Assert.Single(result);
        Assert.Equal("/api/schema-metadata/vocabularies", handler.Requests[1].RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task GetSchemaMetadataListAsync_ParsesList()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.OK, """[{"id":"s1"},{"id":"s2"}]""");

        var result = await client.GetSchemaMetadataListAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal("/api/schema-metadata", handler.Requests[1].RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task GetMySchemaMetadataAsync_ParsesList()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.OK, """[{"id":"s1"}]""");

        var result = await client.GetMySchemaMetadataAsync();

        Assert.Single(result);
        Assert.Equal("/api/schema-metadata/mine", handler.Requests[1].RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task GetSchemaMetadataAsync_NotFound_ReturnsNull()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.NotFound);

        Assert.Null(await client.GetSchemaMetadataAsync("missing"));
    }

    [Fact]
    public async Task GetLatestSchemaMetadataVersionAsync_Found_ReturnsElement()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.OK, """{"version":3}""");

        var result = await client.GetLatestSchemaMetadataVersionAsync("s1");

        Assert.NotNull(result);
        Assert.Equal("/api/schema-metadata/s1/latest", handler.Requests[1].RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task GetSchemaMetadataVersionsAsync_ParsesList()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.OK, """[{"version":1},{"version":2}]""");

        var result = await client.GetSchemaMetadataVersionsAsync("s1");

        Assert.Equal(2, result.Count);
        Assert.Equal("/api/schema-metadata/s1/versions", handler.Requests[1].RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task UpdateSchemaMetadataVersionAsync_Success_SendsPatch()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.OK, """{"version":1}""");

        await client.UpdateSchemaMetadataVersionAsync("s1", "1", """{"description":"x"}""");

        Assert.Equal(HttpMethod.Patch, handler.Requests[1].Method);
        Assert.Equal("/api/schema-metadata/s1/versions/1", handler.Requests[1].RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task UpdateSchemaMetadataVersionAsync_Failure_Throws()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.BadRequest, """{"message":"invalid"}""");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.UpdateSchemaMetadataVersionAsync("s1", "1", "{}"));
    }

    [Fact]
    public async Task DeleteSchemaMetadataVersionAsync_Failure_Throws()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.Forbidden, """{"message":"forbidden"}""");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.DeleteSchemaMetadataVersionAsync("s1", "1"));
    }

    [Fact]
    public async Task GetSchemaMetadataVersionJwtAsync_NotFound_ReturnsNull()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.NotFound);

        Assert.Null(await client.GetSchemaMetadataVersionJwtAsync("s1", "1"));
    }

    [Fact]
    public async Task GetSchemaMetadataVersionJwtAsync_Success_ReturnsRawJwt()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.OK, "eyJhbGciOiJFUzI1NiJ9.payload.signature");

        var result = await client.GetSchemaMetadataVersionJwtAsync("s1", "1");

        Assert.Equal("eyJhbGciOiJFUzI1NiJ9.payload.signature", result);
        Assert.Equal("/api/schema-metadata/s1/versions/1/jwt", handler.Requests[1].RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task ExportSchemaMetadataVersionAsync_NotFound_ReturnsNull()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.NotFound);

        Assert.Null(await client.ExportSchemaMetadataVersionAsync("s1", "1"));
    }

    [Fact]
    public async Task GetSchemaMetadataVersionSchemaAsync_Success_ReturnsBody()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.OK, """{"type":"object"}""");

        var result = await client.GetSchemaMetadataVersionSchemaAsync("s1", "1", "json-schema");

        Assert.Contains("object", result);
        Assert.Equal("/api/schema-metadata/s1/versions/1/schemas/json-schema", handler.Requests[1].RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task GetSchemaMetadataVersionSchemaAsync_NotFound_ReturnsNull()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.NotFound);

        Assert.Null(await client.GetSchemaMetadataVersionSchemaAsync("s1", "1", "jsonld"));
    }

    [Fact]
    public async Task SetSchemaMetadataVersionDeprecationAsync_Success_SendsPatch()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.OK, """{"deprecated":true}""");

        await client.SetSchemaMetadataVersionDeprecationAsync("s1", "1", """{"deprecated":true}""");

        Assert.Equal(HttpMethod.Patch, handler.Requests[1].Method);
        Assert.Equal("/api/schema-metadata/s1/versions/1/deprecation", handler.Requests[1].RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task SetSchemaMetadataVersionDeprecationAsync_Failure_Throws()
    {
        var (client, handler) = TestClientFactory.Create();
        handler.EnqueueToken();
        handler.Enqueue(HttpStatusCode.BadRequest, """{"message":"invalid"}""");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.SetSchemaMetadataVersionDeprecationAsync("s1", "1", "{}"));
    }
}
