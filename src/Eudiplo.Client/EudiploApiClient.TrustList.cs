using System.Net;
using System.Text;
using System.Text.Json;

namespace Eudiplo.Client;

/// <summary>Trust-list management. The trust relationship itself belongs to EUDIPLO — callers
/// only select/import/delete entries.</summary>
public partial class EudiploApiClient
{
    /// <summary>
    /// Lists the tenant's EUDIPLO trust lists — the trust itself belongs to EUDIPLO, not the
    /// caller. Useful for letting a user pick <c>trusted_authorities</c> when composing a
    /// presentation request.
    /// </summary>
    public async Task<IReadOnlyList<EudiploTrustList>> GetTrustListsAsync(CancellationToken ct = default)
    {
        using var resp = await SendWithAuthAsync(
            () => new HttpRequestMessage(HttpMethod.Get, "/api/trust-list"), ct);
        if (!resp.IsSuccessStatusCode) return Array.Empty<EudiploTrustList>();

        var list = new List<EudiploTrustList>();
        foreach (var e in ParseJsonArray(await resp.Content.ReadAsStringAsync(ct)))
        {
            if (!e.TryGetProperty("id", out var i) || i.GetString() is not { Length: > 0 } id) continue;
            var desc   = e.TryGetProperty("description", out var d) ? d.GetString() : null;
            var tenant = e.TryGetProperty("tenantId", out var t) ? t.GetString() : null;
            list.Add(new EudiploTrustList(id, desc ?? id, tenant ?? ""));
        }
        return list;
    }

    /// <summary>Creates a trust list (POST /api/trust-list). JSON = TrustListCreateDto.</summary>
    public async Task CreateTrustListAsync(string json, CancellationToken ct = default)
    {
        using var resp = await SendWithAuthAsync(
            () => new HttpRequestMessage(HttpMethod.Post, "/api/trust-list")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            }, ct);
        var text = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"EUDIPLO trust-list create: HTTP {(int)resp.StatusCode} {text}");
    }

    /// <summary>Deletes a trust list (DELETE /api/trust-list/{id}; 404 is ignored).</summary>
    public async Task DeleteTrustListAsync(string id, CancellationToken ct = default)
    {
        using var resp = await SendWithAuthAsync(
            () => new HttpRequestMessage(HttpMethod.Delete, $"/api/trust-list/{id}"), ct);
        if (!resp.IsSuccessStatusCode && resp.StatusCode != HttpStatusCode.NotFound)
            throw new InvalidOperationException($"EUDIPLO trust-list delete: HTTP {(int)resp.StatusCode}");
    }

    /// <summary>
    /// Public verification JWK of a trust list (e.g. for a "trust list public key JWK" field in
    /// a registrar form). Reads the trust list's keyChainId, exports the key-chain, and returns
    /// only the public fields — the private key (d) is always stripped. null = not available.
    /// </summary>
    public async Task<string?> GetTrustListPublicJwkAsync(string trustListId, CancellationToken ct = default)
    {
        using var tlResp = await SendWithAuthAsync(
            () => new HttpRequestMessage(HttpMethod.Get, $"/api/trust-list/{trustListId}"), ct);
        if (!tlResp.IsSuccessStatusCode) return null;
        using var tlDoc = JsonDocument.Parse(await tlResp.Content.ReadAsStringAsync(ct));
        if (!tlDoc.RootElement.TryGetProperty("keyChainId", out var kcEl) || kcEl.GetString() is not { Length: > 0 } kcId)
            return null;

        using var kcResp = await SendWithAuthAsync(
            () => new HttpRequestMessage(HttpMethod.Get, $"/api/key-chain/{kcId}/export"), ct);
        if (!kcResp.IsSuccessStatusCode) return null;
        using var kcDoc = JsonDocument.Parse(await kcResp.Content.ReadAsStringAsync(ct));
        if (!kcDoc.RootElement.TryGetProperty("key", out var key)) return null;

        // Only carry over public JWK fields — d (private) never.
        var pub = new Dictionary<string, object>();
        foreach (var f in new[] { "kty", "crv", "x", "y", "kid", "alg" })
            if (key.TryGetProperty(f, out var v) && v.GetString() is { } s) pub[f] = s;
        pub["use"] = "sig";
        return JsonSerializer.Serialize(pub);
    }

    /// <summary>Reads a trust list raw (GET /api/trust-list/{id}) — useful for enriching your own
    /// entity representation.</summary>
    public async Task<string?> GetTrustListJsonAsync(string id, CancellationToken ct = default)
    {
        using var resp = await SendWithAuthAsync(
            () => new HttpRequestMessage(HttpMethod.Get, $"/api/trust-list/{id}"), ct);
        if (!resp.IsSuccessStatusCode) return null;
        return await resp.Content.ReadAsStringAsync(ct);
    }

    /// <summary>Writes a trust list in full (PUT /api/trust-list/{id}, TrustListCreateDto shape:
    /// id/description/keyChainId/entities). Takes effect immediately, no restart needed.</summary>
    public async Task UpdateTrustListAsync(string id, string json, CancellationToken ct = default)
    {
        using var resp = await SendWithAuthAsync(
            () => new HttpRequestMessage(HttpMethod.Put, $"/api/trust-list/{id}")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            }, ct);
        var text = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"EUDIPLO trust-list update: HTTP {(int)resp.StatusCode} {text}");
    }

    /// <summary>Exports a trust list (GET /api/trust-list/{id}/export) in a portable format —
    /// for migrating a trust list between EUDIPLO instances or backing it up. null = not found.</summary>
    public async Task<string?> ExportTrustListAsync(string id, CancellationToken ct = default)
    {
        using var resp = await SendWithAuthAsync(
            () => new HttpRequestMessage(HttpMethod.Get, $"/api/trust-list/{id}/export"), ct);
        if (!resp.IsSuccessStatusCode) return null;
        return await resp.Content.ReadAsStringAsync(ct);
    }

    /// <summary>Lists the historical versions of a trust list (GET /api/trust-list/{id}/versions).</summary>
    public async Task<IReadOnlyList<JsonElement>> GetTrustListVersionsAsync(string id, CancellationToken ct = default)
    {
        using var resp = await SendWithAuthAsync(
            () => new HttpRequestMessage(HttpMethod.Get, $"/api/trust-list/{id}/versions"), ct);
        if (!resp.IsSuccessStatusCode) return Array.Empty<JsonElement>();
        return ParseJsonArray(await resp.Content.ReadAsStringAsync(ct));
    }

    /// <summary>Reads a specific historical version of a trust list
    /// (GET /api/trust-list/{id}/versions/{versionId}). null = not found.</summary>
    public async Task<JsonElement?> GetTrustListVersionAsync(string id, string versionId, CancellationToken ct = default)
    {
        using var resp = await SendWithAuthAsync(
            () => new HttpRequestMessage(HttpMethod.Get, $"/api/trust-list/{id}/versions/{versionId}"), ct);
        if (!resp.IsSuccessStatusCode) return null;
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(ct));
        return doc.RootElement.Clone();
    }
}

/// <summary>An EUDIPLO tenant trust list (id, description, tenant) — the basis for the public
/// <c>etsi_tl</c> reference URL <c>{base}/issuers/{tenant}/trust-list/{id}</c>.</summary>
public record EudiploTrustList(string Id, string Description, string TenantId);
