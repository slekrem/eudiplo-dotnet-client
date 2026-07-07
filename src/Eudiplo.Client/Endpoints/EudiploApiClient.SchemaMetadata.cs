namespace Eudiplo.Client;

/// <summary>Schema metadata management — publishing, versioning, and signing custom attestation
/// schemas with a registrar.</summary>
public partial class EudiploApiClient
{
    /// <summary>Signs and submits schema metadata (POST /api/schema-metadata/sign). Returns the
    /// response body (registrar-assigned id etc.) or throws on HTTP failure.</summary>
    public async Task<string> SignSchemaMetadataAsync(string json, CancellationToken ct = default)
    {
        using var resp = await SendWithAuthAsync(
            () => new HttpRequestMessage(HttpMethod.Post, "/api/schema-metadata/sign")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            }, ct);
        var text = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"EUDIPLO schema-metadata/sign: HTTP {(int)resp.StatusCode} {text}");
        return text;
    }

    /// <summary>Signs and submits a new version of existing schema metadata
    /// (POST /api/schema-metadata/sign-version).</summary>
    public async Task<string> SignSchemaMetadataVersionAsync(string json, CancellationToken ct = default)
    {
        using var resp = await SendWithAuthAsync(
            () => new HttpRequestMessage(HttpMethod.Post, "/api/schema-metadata/sign-version")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            }, ct);
        var text = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"EUDIPLO schema-metadata/sign-version: HTTP {(int)resp.StatusCode} {text}");
        return text;
    }

    /// <summary>Lists predefined schema-metadata vocabularies (GET /api/schema-metadata/vocabularies).</summary>
    public async Task<IReadOnlyList<JsonElement>> GetSchemaMetadataVocabulariesAsync(CancellationToken ct = default)
    {
        using var resp = await SendWithAuthAsync(
            () => new HttpRequestMessage(HttpMethod.Get, "/api/schema-metadata/vocabularies"), ct);
        if (!resp.IsSuccessStatusCode) return Array.Empty<JsonElement>();
        return ParseJsonArray(await resp.Content.ReadAsStringAsync(ct));
    }

    /// <summary>Lists all schema metadata (GET /api/schema-metadata).</summary>
    public async Task<IReadOnlyList<JsonElement>> GetSchemaMetadataListAsync(CancellationToken ct = default)
    {
        using var resp = await SendWithAuthAsync(
            () => new HttpRequestMessage(HttpMethod.Get, "/api/schema-metadata"), ct);
        if (!resp.IsSuccessStatusCode) return Array.Empty<JsonElement>();
        return ParseJsonArray(await resp.Content.ReadAsStringAsync(ct));
    }

    /// <summary>Lists schema metadata controlled by the calling user
    /// (GET /api/schema-metadata/mine).</summary>
    public async Task<IReadOnlyList<JsonElement>> GetMySchemaMetadataAsync(CancellationToken ct = default)
    {
        using var resp = await SendWithAuthAsync(
            () => new HttpRequestMessage(HttpMethod.Get, "/api/schema-metadata/mine"), ct);
        if (!resp.IsSuccessStatusCode) return Array.Empty<JsonElement>();
        return ParseJsonArray(await resp.Content.ReadAsStringAsync(ct));
    }

    /// <summary>Reads schema metadata by id (GET /api/schema-metadata/{id}). null = not found.</summary>
    public async Task<JsonElement?> GetSchemaMetadataAsync(string id, CancellationToken ct = default)
    {
        using var resp = await SendWithAuthAsync(
            () => new HttpRequestMessage(HttpMethod.Get, $"/api/schema-metadata/{id}"), ct);
        if (!resp.IsSuccessStatusCode) return null;
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(ct));
        return doc.RootElement.Clone();
    }

    /// <summary>Reads the latest version of schema metadata (GET /api/schema-metadata/{id}/latest).
    /// null = not found.</summary>
    public async Task<JsonElement?> GetLatestSchemaMetadataVersionAsync(string id, CancellationToken ct = default)
    {
        using var resp = await SendWithAuthAsync(
            () => new HttpRequestMessage(HttpMethod.Get, $"/api/schema-metadata/{id}/latest"), ct);
        if (!resp.IsSuccessStatusCode) return null;
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(ct));
        return doc.RootElement.Clone();
    }

    /// <summary>Lists all versions of schema metadata (GET /api/schema-metadata/{id}/versions).</summary>
    public async Task<IReadOnlyList<JsonElement>> GetSchemaMetadataVersionsAsync(string id, CancellationToken ct = default)
    {
        using var resp = await SendWithAuthAsync(
            () => new HttpRequestMessage(HttpMethod.Get, $"/api/schema-metadata/{id}/versions"), ct);
        if (!resp.IsSuccessStatusCode) return Array.Empty<JsonElement>();
        return ParseJsonArray(await resp.Content.ReadAsStringAsync(ct));
    }

    /// <summary>Updates attributes of a specific schema-metadata version
    /// (PATCH /api/schema-metadata/{id}/versions/{version}).</summary>
    public async Task<string> UpdateSchemaMetadataVersionAsync(string id, string version, string json, CancellationToken ct = default)
    {
        using var resp = await SendWithAuthAsync(
            () => new HttpRequestMessage(HttpMethod.Patch, $"/api/schema-metadata/{id}/versions/{version}")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            }, ct);
        var text = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"EUDIPLO schema-metadata version update: HTTP {(int)resp.StatusCode} {text}");
        return text;
    }

    /// <summary>Deletes a specific schema-metadata version
    /// (DELETE /api/schema-metadata/{id}/versions/{version}).</summary>
    public async Task DeleteSchemaMetadataVersionAsync(string id, string version, CancellationToken ct = default)
    {
        using var resp = await SendWithAuthAsync(
            () => new HttpRequestMessage(HttpMethod.Delete, $"/api/schema-metadata/{id}/versions/{version}"), ct);
        var text = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"EUDIPLO schema-metadata version delete: HTTP {(int)resp.StatusCode} {text}");
    }

    /// <summary>Reads the signed JWT for a schema-metadata version
    /// (GET /api/schema-metadata/{id}/versions/{version}/jwt). null = not found.</summary>
    public async Task<string?> GetSchemaMetadataVersionJwtAsync(string id, string version, CancellationToken ct = default)
    {
        using var resp = await SendWithAuthAsync(
            () => new HttpRequestMessage(HttpMethod.Get, $"/api/schema-metadata/{id}/versions/{version}/jwt"), ct);
        if (!resp.IsSuccessStatusCode) return null;
        return await resp.Content.ReadAsStringAsync(ct);
    }

    /// <summary>Exports a schema-metadata version in catalog format
    /// (GET /api/schema-metadata/{id}/versions/{version}/export). null = not found.</summary>
    public async Task<string?> ExportSchemaMetadataVersionAsync(string id, string version, CancellationToken ct = default)
    {
        using var resp = await SendWithAuthAsync(
            () => new HttpRequestMessage(HttpMethod.Get, $"/api/schema-metadata/{id}/versions/{version}/export"), ct);
        if (!resp.IsSuccessStatusCode) return null;
        return await resp.Content.ReadAsStringAsync(ct);
    }

    /// <summary>Reads the schema content for a specific format (e.g. "json-schema", "jsonld")
    /// (GET /api/schema-metadata/{id}/versions/{version}/schemas/{format}). null = not found.</summary>
    public async Task<string?> GetSchemaMetadataVersionSchemaAsync(string id, string version, string format, CancellationToken ct = default)
    {
        using var resp = await SendWithAuthAsync(
            () => new HttpRequestMessage(HttpMethod.Get, $"/api/schema-metadata/{id}/versions/{version}/schemas/{format}"), ct);
        if (!resp.IsSuccessStatusCode) return null;
        return await resp.Content.ReadAsStringAsync(ct);
    }

    /// <summary>Sets the deprecation status of a schema-metadata version
    /// (PATCH /api/schema-metadata/{id}/versions/{version}/deprecation).</summary>
    public async Task<string> SetSchemaMetadataVersionDeprecationAsync(string id, string version, string json, CancellationToken ct = default)
    {
        using var resp = await SendWithAuthAsync(
            () => new HttpRequestMessage(HttpMethod.Patch, $"/api/schema-metadata/{id}/versions/{version}/deprecation")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            }, ct);
        var text = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"EUDIPLO schema-metadata deprecation set: HTTP {(int)resp.StatusCode} {text}");
        return text;
    }
}
