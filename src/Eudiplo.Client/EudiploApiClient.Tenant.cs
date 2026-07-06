using System.Text;
using System.Text.Json;

namespace Eudiplo.Client;

/// <summary>
/// Tenant management (multi-tenant setups — separate relying parties with full isolation).
/// Requires the tenant-less root client (role <c>tenants:manage</c>) — construct this
/// <see cref="EudiploApiClient"/> with the root client's credentials instead of a regular
/// tenant's credentials to use these methods.
/// </summary>
public partial class EudiploApiClient
{
    /// <summary>Lists all EUDIPLO tenants (raw).</summary>
    public async Task<IReadOnlyList<JsonElement>> GetTenantsAsync(CancellationToken ct = default)
    {
        using var resp = await SendWithAuthAsync(
            () => new HttpRequestMessage(HttpMethod.Get, "/api/tenant"), ct);
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

    /// <summary>Creates a new tenant (POST /api/tenant). If the body includes <c>roles</c>,
    /// EUDIPLO automatically creates a bootstrap client "{id}-admin" with those roles (plus
    /// <c>clients:manage</c>) and returns its secret in plaintext — visible only this one time.</summary>
    public async Task<string> CreateTenantAsync(string json, CancellationToken ct = default)
    {
        using var resp = await SendWithAuthAsync(
            () => new HttpRequestMessage(HttpMethod.Post, "/api/tenant")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            }, ct);
        var text = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"EUDIPLO tenant create: HTTP {(int)resp.StatusCode} {text}");
        return text;
    }

    /// <summary>Deletes a tenant entirely (DELETE /api/tenant/{id}) — including all of its
    /// clients/key-chains/configs. Not reversible.</summary>
    public async Task DeleteTenantAsync(string id, CancellationToken ct = default)
    {
        using var resp = await SendWithAuthAsync(
            () => new HttpRequestMessage(HttpMethod.Delete, $"/api/tenant/{id}"), ct);
        var text = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"EUDIPLO tenant delete: HTTP {(int)resp.StatusCode} {text}");
    }
}
