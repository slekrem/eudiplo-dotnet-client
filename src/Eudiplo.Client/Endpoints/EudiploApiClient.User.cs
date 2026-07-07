namespace Eudiplo.Client;

/// <summary>
/// Human-user management within the current tenant (distinct from OAuth2 clients — these are
/// accounts for people who log into EUDIPLO's own admin UI).
/// </summary>
public partial class EudiploApiClient
{
    /// <summary>Lists the tenant's managed users (GET /api/user).</summary>
    public async Task<IReadOnlyList<JsonElement>> GetUsersAsync(CancellationToken ct = default)
    {
        using var resp = await SendWithAuthAsync(
            () => new HttpRequestMessage(HttpMethod.Get, "/api/user"), ct);
        if (!resp.IsSuccessStatusCode) return Array.Empty<JsonElement>();
        return ParseJsonArray(await resp.Content.ReadAsStringAsync(ct));
    }

    /// <summary>Reads a single managed user (GET /api/user/{id}). null = not found.</summary>
    public async Task<JsonElement?> GetUserAsync(string id, CancellationToken ct = default)
    {
        using var resp = await SendWithAuthAsync(
            () => new HttpRequestMessage(HttpMethod.Get, $"/api/user/{id}"), ct);
        if (!resp.IsSuccessStatusCode) return null;
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(ct));
        return doc.RootElement.Clone();
    }

    /// <summary>Creates a managed user (POST /api/user).</summary>
    public async Task<string> CreateUserAsync(string json, CancellationToken ct = default)
    {
        using var resp = await SendWithAuthAsync(
            () => new HttpRequestMessage(HttpMethod.Post, "/api/user")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            }, ct);
        var text = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"EUDIPLO user create: HTTP {(int)resp.StatusCode} {text}");
        return text;
    }

    /// <summary>Updates a managed user (PATCH /api/user/{id}).</summary>
    public async Task<string> UpdateUserAsync(string id, string json, CancellationToken ct = default)
    {
        using var resp = await SendWithAuthAsync(
            () => new HttpRequestMessage(HttpMethod.Patch, $"/api/user/{id}")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            }, ct);
        var text = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"EUDIPLO user update: HTTP {(int)resp.StatusCode} {text}");
        return text;
    }

    /// <summary>Deletes a managed user (DELETE /api/user/{id}).</summary>
    public async Task DeleteUserAsync(string id, CancellationToken ct = default)
    {
        using var resp = await SendWithAuthAsync(
            () => new HttpRequestMessage(HttpMethod.Delete, $"/api/user/{id}"), ct);
        var text = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"EUDIPLO user delete: HTTP {(int)resp.StatusCode} {text}");
    }
}
