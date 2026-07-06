using System.Net;
using System.Text;
using System.Text.Json;

namespace Eudiplo.Client;

/// <summary>
/// Presentation-config (verifier-config) management. Also useful for dynamically cloning an
/// existing config with different requested claims (e.g. for a self-test flow).
/// </summary>
public partial class EudiploApiClient
{
    /// <summary>Reads a verifier (presentation) config. JSON body or null.</summary>
    public async Task<string?> GetVerifierConfigJsonAsync(string id, CancellationToken ct = default)
    {
        using var resp = await SendWithAuthAsync(
            () => new HttpRequestMessage(HttpMethod.Get, $"/api/verifier/config/{id}"), ct);
        if (!resp.IsSuccessStatusCode) return null;
        return await resp.Content.ReadAsStringAsync(ct);
    }

    /// <summary>Creates a verifier config (POST /api/verifier/config).</summary>
    public async Task PostVerifierConfigAsync(string configJson, CancellationToken ct = default)
    {
        using var resp = await SendWithAuthAsync(
            () => new HttpRequestMessage(HttpMethod.Post, "/api/verifier/config")
            {
                Content = new StringContent(configJson, Encoding.UTF8, "application/json"),
            }, ct);
        var text = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"EUDIPLO verifier-config create: HTTP {(int)resp.StatusCode} {text}");
    }

    /// <summary>Updates a verifier config (PATCH /api/verifier/config/{id}, partial update).</summary>
    public async Task UpdateVerifierConfigAsync(string id, string configJson, CancellationToken ct = default)
    {
        using var resp = await SendWithAuthAsync(
            () => new HttpRequestMessage(HttpMethod.Patch, $"/api/verifier/config/{id}")
            {
                Content = new StringContent(configJson, Encoding.UTF8, "application/json"),
            }, ct);
        var text = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"EUDIPLO verifier-config update: HTTP {(int)resp.StatusCode} {text}");
    }

    /// <summary>Deletes a verifier config (idempotent — 404 is ignored, any other error status
    /// throws instead of being silently treated as success).</summary>
    public async Task DeleteVerifierConfigAsync(string id, CancellationToken ct = default)
    {
        using var resp = await SendWithAuthAsync(
            () => new HttpRequestMessage(HttpMethod.Delete, $"/api/verifier/config/{id}"), ct);
        if (!resp.IsSuccessStatusCode && resp.StatusCode != HttpStatusCode.NotFound)
            throw new InvalidOperationException($"EUDIPLO verifier-config delete: HTTP {(int)resp.StatusCode}");
    }

    /// <summary>Lists all verifier (presentation) configs of the tenant (GET /api/verifier/config).</summary>
    public async Task<IReadOnlyList<JsonElement>> GetVerifierConfigsAsync(CancellationToken ct = default)
    {
        using var resp = await SendWithAuthAsync(
            () => new HttpRequestMessage(HttpMethod.Get, "/api/verifier/config"), ct);
        if (!resp.IsSuccessStatusCode) return Array.Empty<JsonElement>();
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(ct));
        var root = doc.RootElement;
        var arr = root.ValueKind == JsonValueKind.Array ? root
            : root.TryGetProperty("items", out var it) ? it : default;
        if (arr.ValueKind != JsonValueKind.Array) return Array.Empty<JsonElement>();
        var list = new List<JsonElement>();
        foreach (var e in arr.EnumerateArray()) list.Add(e.Clone());
        return list;
    }

    /// <summary>Forces re-resolution of a config's registration certificate against the
    /// registrar, bypassing the cache (POST /api/verifier/config/{id}/registration-cert/reissue).</summary>
    public async Task ReissueRegistrationCertificateAsync(string id, CancellationToken ct = default)
    {
        using var resp = await SendWithAuthAsync(
            () => new HttpRequestMessage(HttpMethod.Post, $"/api/verifier/config/{id}/registration-cert/reissue"), ct);
        var text = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"EUDIPLO registration-cert reissue: HTTP {(int)resp.StatusCode} {text}");
    }
}
