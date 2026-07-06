namespace Eudiplo.Client;

/// <summary>Service-level introspection — root-level endpoints, not tenant business logic.</summary>
public partial class EudiploApiClient
{
    /// <summary>Reads the running EUDIPLO service version (GET /version, JWT-guarded).
    /// null = non-success status.</summary>
    public async Task<string?> GetVersionAsync(CancellationToken ct = default)
    {
        using var resp = await SendWithAuthAsync(
            () => new HttpRequestMessage(HttpMethod.Get, "/version"), ct);
        if (!resp.IsSuccessStatusCode) return null;
        return await resp.Content.ReadAsStringAsync(ct);
    }

    /// <summary>Reads the frontend runtime config (GET /frontend-config, JWT-guarded).
    /// null = non-success status.</summary>
    public async Task<string?> GetFrontendConfigAsync(CancellationToken ct = default)
    {
        using var resp = await SendWithAuthAsync(
            () => new HttpRequestMessage(HttpMethod.Get, "/frontend-config"), ct);
        if (!resp.IsSuccessStatusCode) return null;
        return await resp.Content.ReadAsStringAsync(ct);
    }
}
