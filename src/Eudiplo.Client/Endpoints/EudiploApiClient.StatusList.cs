namespace Eudiplo.Client;

/// <summary>
/// Token Status List (IETF) configuration and management — controls how EUDIPLO tracks and
/// publishes revocation status for issued credentials. Distinct from
/// <see cref="RevokeSessionAsync"/>, which flips the status bit for one already-issued
/// credential; the methods here manage the status lists themselves.
/// </summary>
public partial class EudiploApiClient
{
    /// <summary>Reads the tenant's status-list configuration (GET /api/status-list-config).
    /// null = none configured (falls back to the default).</summary>
    public async Task<string?> GetStatusListConfigAsync(CancellationToken ct = default)
    {
        using var resp = await SendWithAuthAsync(
            () => new HttpRequestMessage(HttpMethod.Get, "/api/status-list-config"), ct);
        if (!resp.IsSuccessStatusCode) return null;
        return await resp.Content.ReadAsStringAsync(ct);
    }

    /// <summary>Creates or replaces the tenant's status-list configuration
    /// (PUT /api/status-list-config).</summary>
    public async Task<string> SetStatusListConfigAsync(string json, CancellationToken ct = default)
    {
        using var resp = await SendWithAuthAsync(
            () => new HttpRequestMessage(HttpMethod.Put, "/api/status-list-config")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            }, ct);
        var text = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"EUDIPLO status-list-config set: HTTP {(int)resp.StatusCode} {text}");
        return text;
    }

    /// <summary>Resets the tenant's status-list configuration to the default
    /// (DELETE /api/status-list-config; idempotent — 404 is ignored).</summary>
    public async Task ResetStatusListConfigAsync(CancellationToken ct = default)
    {
        using var resp = await SendWithAuthAsync(
            () => new HttpRequestMessage(HttpMethod.Delete, "/api/status-list-config"), ct);
        if (!resp.IsSuccessStatusCode && resp.StatusCode != HttpStatusCode.NotFound)
            throw new InvalidOperationException($"EUDIPLO status-list-config reset: HTTP {(int)resp.StatusCode}");
    }

    /// <summary>Lists the tenant's status lists (GET /api/status-lists).</summary>
    public async Task<IReadOnlyList<JsonElement>> GetStatusListsAsync(CancellationToken ct = default)
    {
        using var resp = await SendWithAuthAsync(
            () => new HttpRequestMessage(HttpMethod.Get, "/api/status-lists"), ct);
        if (!resp.IsSuccessStatusCode) return Array.Empty<JsonElement>();
        return ParseJsonArray(await resp.Content.ReadAsStringAsync(ct));
    }

    /// <summary>Reads a single status list (GET /api/status-lists/{listId}). null = not found.</summary>
    public async Task<JsonElement?> GetStatusListAsync(string listId, CancellationToken ct = default)
    {
        using var resp = await SendWithAuthAsync(
            () => new HttpRequestMessage(HttpMethod.Get, $"/api/status-lists/{listId}"), ct);
        if (!resp.IsSuccessStatusCode) return null;
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(ct));
        return doc.RootElement.Clone();
    }

    /// <summary>Creates a status list (POST /api/status-lists).</summary>
    public async Task<string> CreateStatusListAsync(string json, CancellationToken ct = default)
    {
        using var resp = await SendWithAuthAsync(
            () => new HttpRequestMessage(HttpMethod.Post, "/api/status-lists")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            }, ct);
        var text = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"EUDIPLO status-list create: HTTP {(int)resp.StatusCode} {text}");
        return text;
    }

    /// <summary>Updates a status list (PATCH /api/status-lists/{listId}).</summary>
    public async Task<string> UpdateStatusListAsync(string listId, string json, CancellationToken ct = default)
    {
        using var resp = await SendWithAuthAsync(
            () => new HttpRequestMessage(HttpMethod.Patch, $"/api/status-lists/{listId}")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            }, ct);
        var text = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"EUDIPLO status-list update: HTTP {(int)resp.StatusCode} {text}");
        return text;
    }

    /// <summary>Deletes a status list (DELETE /api/status-lists/{listId}).</summary>
    public async Task DeleteStatusListAsync(string listId, CancellationToken ct = default)
    {
        using var resp = await SendWithAuthAsync(
            () => new HttpRequestMessage(HttpMethod.Delete, $"/api/status-lists/{listId}"), ct);
        var text = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"EUDIPLO status-list delete: HTTP {(int)resp.StatusCode} {text}");
    }
}
