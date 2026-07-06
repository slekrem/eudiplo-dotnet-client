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
        return ParseJsonArray(await resp.Content.ReadAsStringAsync(ct));
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

    /// <summary>Resolves external issuer metadata (POST /api/verifier/config/issuer-metadata/resolve)
    /// — used when composing a presentation config that trusts a specific external issuer, to look
    /// up that issuer's metadata (e.g. by URL) instead of entering it by hand.</summary>
    public async Task<string> ResolveIssuerMetadataAsync(string json, CancellationToken ct = default)
    {
        using var resp = await SendWithAuthAsync(
            () => new HttpRequestMessage(HttpMethod.Post, "/api/verifier/config/issuer-metadata/resolve")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            }, ct);
        var text = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"EUDIPLO issuer-metadata resolve: HTTP {(int)resp.StatusCode} {text}");
        return text;
    }

    /// <summary>Resolves external schema metadata (POST /api/verifier/config/schema-metadata/resolve)
    /// — looks up a published schema (e.g. by its registrar-assigned id/URL) for use when composing
    /// a presentation config's requested claims.</summary>
    public async Task<string> ResolveSchemaMetadataAsync(string json, CancellationToken ct = default)
    {
        using var resp = await SendWithAuthAsync(
            () => new HttpRequestMessage(HttpMethod.Post, "/api/verifier/config/schema-metadata/resolve")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            }, ct);
        var text = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"EUDIPLO schema-metadata resolve: HTTP {(int)resp.StatusCode} {text}");
        return text;
    }

    /// <summary>Lists schema metadata available from the registrar's catalog
    /// (GET /api/verifier/config/schema-metadata/catalog) — for picking an existing published
    /// schema when composing a presentation config, instead of resolving one by hand.</summary>
    public async Task<IReadOnlyList<JsonElement>> GetSchemaMetadataCatalogAsync(CancellationToken ct = default)
    {
        using var resp = await SendWithAuthAsync(
            () => new HttpRequestMessage(HttpMethod.Get, "/api/verifier/config/schema-metadata/catalog"), ct);
        if (!resp.IsSuccessStatusCode) return Array.Empty<JsonElement>();
        return ParseJsonArray(await resp.Content.ReadAsStringAsync(ct));
    }
}
