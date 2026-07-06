using System.Net;
using System.Text;

namespace Eudiplo.Client;

/// <summary>Session storage configuration (e.g. backend/TTL for how EUDIPLO persists
/// in-flight issuance/presentation sessions).</summary>
public partial class EudiploApiClient
{
    /// <summary>Reads the tenant's session storage configuration (GET /api/session-config).
    /// null = none configured (falls back to the default).</summary>
    public async Task<string?> GetSessionConfigAsync(CancellationToken ct = default)
    {
        using var resp = await SendWithAuthAsync(
            () => new HttpRequestMessage(HttpMethod.Get, "/api/session-config"), ct);
        if (!resp.IsSuccessStatusCode) return null;
        return await resp.Content.ReadAsStringAsync(ct);
    }

    /// <summary>Creates or replaces the tenant's session storage configuration
    /// (PUT /api/session-config).</summary>
    public async Task<string> SetSessionConfigAsync(string json, CancellationToken ct = default)
    {
        using var resp = await SendWithAuthAsync(
            () => new HttpRequestMessage(HttpMethod.Put, "/api/session-config")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            }, ct);
        var text = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"EUDIPLO session-config set: HTTP {(int)resp.StatusCode} {text}");
        return text;
    }

    /// <summary>Resets the tenant's session storage configuration to the default
    /// (DELETE /api/session-config; idempotent — 404 is ignored).</summary>
    public async Task ResetSessionConfigAsync(CancellationToken ct = default)
    {
        using var resp = await SendWithAuthAsync(
            () => new HttpRequestMessage(HttpMethod.Delete, "/api/session-config"), ct);
        if (!resp.IsSuccessStatusCode && resp.StatusCode != HttpStatusCode.NotFound)
            throw new InvalidOperationException($"EUDIPLO session-config reset: HTTP {(int)resp.StatusCode}");
    }
}
