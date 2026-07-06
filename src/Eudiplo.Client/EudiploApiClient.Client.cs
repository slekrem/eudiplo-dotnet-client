using System.Text;
using System.Text.Json;

namespace Eudiplo.Client;

/// <summary>
/// OAuth2 client management for the current tenant. Requires the <c>clients:manage</c> role.
/// Manages the OAuth2 clients of the tenant this <see cref="EudiploApiClient"/> was
/// constructed for — not the root client itself (which is tenant-less and managed via
/// <c>/api/tenant</c>, not <c>/api/client</c>).
/// </summary>
public partial class EudiploApiClient
{
    /// <summary>Lists the clients of the current tenant (raw, without secrets).</summary>
    public async Task<IReadOnlyList<JsonElement>> GetClientsAsync(CancellationToken ct = default)
    {
        using var resp = await SendWithAuthAsync(
            () => new HttpRequestMessage(HttpMethod.Get, "/api/client"), ct);
        if (!resp.IsSuccessStatusCode) return Array.Empty<JsonElement>();
        return ParseJsonArray(await resp.Content.ReadAsStringAsync(ct));
    }

    /// <summary>Reads a single client (raw, without secret). null = not found.</summary>
    public async Task<JsonElement?> GetClientAsync(string clientId, CancellationToken ct = default)
    {
        using var resp = await SendWithAuthAsync(
            () => new HttpRequestMessage(HttpMethod.Get, $"/api/client/{clientId}"), ct);
        if (!resp.IsSuccessStatusCode) return null;
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(ct));
        return doc.RootElement.Clone();
    }

    /// <summary>Creates a new client (POST /api/client). The response contains
    /// <c>clientSecret</c> in plaintext — visible only this one time.</summary>
    public async Task<string> CreateClientAsync(string json, CancellationToken ct = default)
    {
        using var resp = await SendWithAuthAsync(
            () => new HttpRequestMessage(HttpMethod.Post, "/api/client")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            }, ct);
        var text = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"EUDIPLO client create: HTTP {(int)resp.StatusCode} {text}");
        return text;
    }

    /// <summary>Updates a client's description/roles (PATCH /api/client/{id}).</summary>
    public async Task UpdateClientAsync(string clientId, string json, CancellationToken ct = default)
    {
        using var resp = await SendWithAuthAsync(
            () => new HttpRequestMessage(HttpMethod.Patch, $"/api/client/{clientId}")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            }, ct);
        var text = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"EUDIPLO client update: HTTP {(int)resp.StatusCode} {text}");
    }

    /// <summary>Deletes a client (DELETE /api/client/{id}).</summary>
    public async Task DeleteClientAsync(string clientId, CancellationToken ct = default)
    {
        using var resp = await SendWithAuthAsync(
            () => new HttpRequestMessage(HttpMethod.Delete, $"/api/client/{clientId}"), ct);
        var text = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"EUDIPLO client delete: HTTP {(int)resp.StatusCode} {text}");
    }

    /// <summary>Rotates a client's secret (POST /api/client/{id}/rotate-secret).
    /// Returns the new secret in plaintext — visible only this one time.</summary>
    public async Task<string> RotateClientSecretAsync(string clientId, CancellationToken ct = default)
    {
        using var resp = await SendWithAuthAsync(
            () => new HttpRequestMessage(HttpMethod.Post, $"/api/client/{clientId}/rotate-secret"), ct);
        var text = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"EUDIPLO client rotate-secret: HTTP {(int)resp.StatusCode} {text}");
        using var doc = JsonDocument.Parse(text);
        return doc.RootElement.GetProperty("secret").GetString()
            ?? throw new InvalidOperationException("EUDIPLO rotate-secret response had no secret");
    }
}
