using System.Text;
using System.Text.Json;

namespace Eudiplo.Client;

/// <summary>
/// Attribute-provider management — external sources EUDIPLO can call to enrich or supply
/// credential claims at issuance time (e.g. resolving a claim from a backend system instead of
/// requiring it inline in the issuance offer).
/// </summary>
public partial class EudiploApiClient
{
    /// <summary>Lists the tenant's attribute providers (GET /api/issuer/attribute-providers).</summary>
    public async Task<IReadOnlyList<JsonElement>> GetAttributeProvidersAsync(CancellationToken ct = default)
    {
        using var resp = await SendWithAuthAsync(
            () => new HttpRequestMessage(HttpMethod.Get, "/api/issuer/attribute-providers"), ct);
        if (!resp.IsSuccessStatusCode) return Array.Empty<JsonElement>();
        return ParseJsonArray(await resp.Content.ReadAsStringAsync(ct));
    }

    /// <summary>Reads a single attribute provider (GET /api/issuer/attribute-providers/{id}).
    /// null = not found.</summary>
    public async Task<JsonElement?> GetAttributeProviderAsync(string id, CancellationToken ct = default)
    {
        using var resp = await SendWithAuthAsync(
            () => new HttpRequestMessage(HttpMethod.Get, $"/api/issuer/attribute-providers/{id}"), ct);
        if (!resp.IsSuccessStatusCode) return null;
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(ct));
        return doc.RootElement.Clone();
    }

    /// <summary>Creates an attribute provider (POST /api/issuer/attribute-providers).</summary>
    public async Task<string> CreateAttributeProviderAsync(string json, CancellationToken ct = default)
    {
        using var resp = await SendWithAuthAsync(
            () => new HttpRequestMessage(HttpMethod.Post, "/api/issuer/attribute-providers")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            }, ct);
        var text = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"EUDIPLO attribute-provider create: HTTP {(int)resp.StatusCode} {text}");
        return text;
    }

    /// <summary>Updates an attribute provider (PATCH /api/issuer/attribute-providers/{id}).</summary>
    public async Task<string> UpdateAttributeProviderAsync(string id, string json, CancellationToken ct = default)
    {
        using var resp = await SendWithAuthAsync(
            () => new HttpRequestMessage(HttpMethod.Patch, $"/api/issuer/attribute-providers/{id}")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            }, ct);
        var text = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"EUDIPLO attribute-provider update: HTTP {(int)resp.StatusCode} {text}");
        return text;
    }

    /// <summary>Deletes an attribute provider (DELETE /api/issuer/attribute-providers/{id}).</summary>
    public async Task DeleteAttributeProviderAsync(string id, CancellationToken ct = default)
    {
        using var resp = await SendWithAuthAsync(
            () => new HttpRequestMessage(HttpMethod.Delete, $"/api/issuer/attribute-providers/{id}"), ct);
        var text = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"EUDIPLO attribute-provider delete: HTTP {(int)resp.StatusCode} {text}");
    }
}
