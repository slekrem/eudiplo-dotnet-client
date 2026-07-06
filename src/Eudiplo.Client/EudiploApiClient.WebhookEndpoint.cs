using System.Text;
using System.Text.Json;

namespace Eudiplo.Client;

/// <summary>
/// Webhook-endpoint management — lets EUDIPLO notify an external system (e.g. your backend)
/// about issuance/presentation lifecycle events, instead of only relying on session polling.
/// </summary>
public partial class EudiploApiClient
{
    /// <summary>Lists the tenant's webhook endpoints (GET /api/issuer/webhook-endpoints).</summary>
    public async Task<IReadOnlyList<JsonElement>> GetWebhookEndpointsAsync(CancellationToken ct = default)
    {
        using var resp = await SendWithAuthAsync(
            () => new HttpRequestMessage(HttpMethod.Get, "/api/issuer/webhook-endpoints"), ct);
        if (!resp.IsSuccessStatusCode) return Array.Empty<JsonElement>();
        return ParseJsonArray(await resp.Content.ReadAsStringAsync(ct));
    }

    /// <summary>Reads a single webhook endpoint (GET /api/issuer/webhook-endpoints/{id}).
    /// null = not found.</summary>
    public async Task<JsonElement?> GetWebhookEndpointAsync(string id, CancellationToken ct = default)
    {
        using var resp = await SendWithAuthAsync(
            () => new HttpRequestMessage(HttpMethod.Get, $"/api/issuer/webhook-endpoints/{id}"), ct);
        if (!resp.IsSuccessStatusCode) return null;
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(ct));
        return doc.RootElement.Clone();
    }

    /// <summary>Creates a webhook endpoint (POST /api/issuer/webhook-endpoints).</summary>
    public async Task<string> CreateWebhookEndpointAsync(string json, CancellationToken ct = default)
    {
        using var resp = await SendWithAuthAsync(
            () => new HttpRequestMessage(HttpMethod.Post, "/api/issuer/webhook-endpoints")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            }, ct);
        var text = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"EUDIPLO webhook-endpoint create: HTTP {(int)resp.StatusCode} {text}");
        return text;
    }

    /// <summary>Updates a webhook endpoint (PATCH /api/issuer/webhook-endpoints/{id}).</summary>
    public async Task<string> UpdateWebhookEndpointAsync(string id, string json, CancellationToken ct = default)
    {
        using var resp = await SendWithAuthAsync(
            () => new HttpRequestMessage(HttpMethod.Patch, $"/api/issuer/webhook-endpoints/{id}")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            }, ct);
        var text = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"EUDIPLO webhook-endpoint update: HTTP {(int)resp.StatusCode} {text}");
        return text;
    }

    /// <summary>Deletes a webhook endpoint (DELETE /api/issuer/webhook-endpoints/{id}).</summary>
    public async Task DeleteWebhookEndpointAsync(string id, CancellationToken ct = default)
    {
        using var resp = await SendWithAuthAsync(
            () => new HttpRequestMessage(HttpMethod.Delete, $"/api/issuer/webhook-endpoints/{id}"), ct);
        var text = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"EUDIPLO webhook-endpoint delete: HTTP {(int)resp.StatusCode} {text}");
    }
}
