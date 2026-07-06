using System.Text;
using System.Text.Json;

namespace Eudiplo.Client;

/// <summary>
/// Issuer / credential-config management (OID4VCI). Lets you upsert credential configs,
/// create issuance offers, revoke issued credentials via EUDIPLO's native status list, and
/// read/write the tenant's issuer config.
/// </summary>
public partial class EudiploApiClient
{
    /// <summary>
    /// Creates or completely replaces a credential config (DELETE-then-POST). EUDIPLO also
    /// exposes a PATCH /issuer/credentials/{id} (partial update), but DELETE-then-POST is used
    /// deliberately here: if docType/display/fields vary significantly between calls, a PATCH
    /// would only overwrite the fields you send and could leave stale fields from a previous,
    /// differently-shaped config behind — DELETE-then-POST guarantees a clean slate.
    /// <paramref name="configJson"/> matches EUDIPLO's <c>CredentialConfigCreate</c> shape.
    /// </summary>
    public async Task UpsertCredentialConfigAsync(string id, string configJson, CancellationToken ct = default)
    {
        using (var del = await SendWithAuthAsync(
            () => new HttpRequestMessage(HttpMethod.Delete, $"/api/issuer/credentials/{id}"), ct))
        { /* 404 = didn't exist yet — fine */ }

        using var resp = await SendWithAuthAsync(
            () => new HttpRequestMessage(HttpMethod.Post, "/api/issuer/credentials")
            {
                Content = new StringContent(configJson, Encoding.UTF8, "application/json"),
            }, ct);
        var text = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"EUDIPLO credential-config upsert: HTTP {(int)resp.StatusCode} {text}");
    }

    /// <summary>
    /// Creates an issuance offer (POST /api/issuer/offer, requires the <c>issuance:offer</c>
    /// role). Returns the <c>openid-credential-offer://</c> URI (for a QR code) and the
    /// EUDIPLO session id (for polling/revocation).
    /// </summary>
    public async Task<(string Uri, string SessionId)> CreateIssuanceOfferAsync(string offerJson, CancellationToken ct = default)
    {
        using var resp = await SendWithAuthAsync(
            () => new HttpRequestMessage(HttpMethod.Post, "/api/issuer/offer")
            {
                Content = new StringContent(offerJson, Encoding.UTF8, "application/json"),
            }, ct);
        var text = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"EUDIPLO issuer offer: HTTP {(int)resp.StatusCode} {text}");

        using var doc = JsonDocument.Parse(text);
        var root = doc.RootElement;
        if (!root.TryGetProperty("uri", out var u) || u.GetString() is not { } uri
            || !root.TryGetProperty("session", out var s) || s.GetString() is not { } sid)
            throw new InvalidOperationException("EUDIPLO issuer-offer response had no uri/session");
        return (uri, sid);
    }

    /// <summary>
    /// Sets the token-status-list status of an issued credential (POST /api/session/revoke).
    /// <paramref name="status"/>: 1 = revoked, 0 = valid (IETF Token Status List).
    /// </summary>
    public async Task RevokeSessionAsync(string sessionId, string credentialConfigurationId, int status, CancellationToken ct = default)
    {
        var body = JsonSerializer.Serialize(new { sessionId, credentialConfigurationId, status });
        using var resp = await SendWithAuthAsync(
            () => new HttpRequestMessage(HttpMethod.Post, "/api/session/revoke")
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            }, ct);
        var text = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"EUDIPLO session revoke: HTTP {(int)resp.StatusCode} {text}");
    }

    /// <summary>Reads the issuer config (GET /api/issuer/config) as raw JSON.</summary>
    public async Task<string?> GetIssuerConfigJsonAsync(CancellationToken ct = default)
    {
        using var resp = await SendWithAuthAsync(
            () => new HttpRequestMessage(HttpMethod.Get, "/api/issuer/config"), ct);
        if (!resp.IsSuccessStatusCode) return null;
        return await resp.Content.ReadAsStringAsync(ct);
    }

    /// <summary>Lists all credential configs of the tenant (GET /api/issuer/credentials).</summary>
    public async Task<IReadOnlyList<JsonElement>> GetCredentialConfigsAsync(CancellationToken ct = default)
    {
        using var resp = await SendWithAuthAsync(
            () => new HttpRequestMessage(HttpMethod.Get, "/api/issuer/credentials"), ct);
        if (!resp.IsSuccessStatusCode) return Array.Empty<JsonElement>();
        return ParseJsonArray(await resp.Content.ReadAsStringAsync(ct));
    }

    /// <summary>Reads a single credential config (GET /api/issuer/credentials/{id}). null = not found.</summary>
    public async Task<JsonElement?> GetCredentialConfigAsync(string id, CancellationToken ct = default)
    {
        using var resp = await SendWithAuthAsync(
            () => new HttpRequestMessage(HttpMethod.Get, $"/api/issuer/credentials/{id}"), ct);
        if (!resp.IsSuccessStatusCode) return null;
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(ct));
        return doc.RootElement.Clone();
    }

    /// <summary>Partially updates a credential config (PATCH /api/issuer/credentials/{id}) — only
    /// the fields you send are changed, unlike <see cref="UpsertCredentialConfigAsync"/> which
    /// replaces the config entirely. Prefer this for small, additive edits.</summary>
    public async Task<string> PatchCredentialConfigAsync(string id, string json, CancellationToken ct = default)
    {
        using var resp = await SendWithAuthAsync(
            () => new HttpRequestMessage(HttpMethod.Patch, $"/api/issuer/credentials/{id}")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            }, ct);
        var text = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"EUDIPLO credential-config patch: HTTP {(int)resp.StatusCode} {text}");
        return text;
    }

    /// <summary>Writes the issuer config (POST /api/issuer/config — upsert). EUDIPLO merges
    /// server-side with the existing config (fields you don't send are left as-is, there is no
    /// reset to defaults) — sending the full body is still correct because tenantId/createdAt/
    /// updatedAt are server-assigned fields that must be stripped before re-posting, not because
    /// of a reset risk.</summary>
    public async Task SetIssuerConfigAsync(string json, CancellationToken ct = default)
    {
        using var resp = await SendWithAuthAsync(
            () => new HttpRequestMessage(HttpMethod.Post, "/api/issuer/config")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            }, ct);
        var text = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"EUDIPLO issuer-config set: HTTP {(int)resp.StatusCode} {text}");
    }
}
