namespace Eudiplo.Client;

/// <summary>
/// Registrar integration (native username/password linkage). EUDIPLO can authenticate itself
/// against a registrar (OIDC password grant, credentials stored per tenant) and automatically
/// obtain access certificates — replacing the manual "register externally, then attach the
/// cert" flow for tenants that have a registrar config configured.
/// </summary>
public partial class EudiploApiClient
{
    /// <summary>Reads the tenant's registrar config (raw). Note: EUDIPLO's response DTO only
    /// masks the ROPC password (as a <c>hasPassword</c> boolean) — <c>clientSecret</c> comes
    /// back in plaintext (verified against EUDIPLO's source). null = no config stored (404).</summary>
    public async Task<string?> GetRegistrarConfigAsync(CancellationToken ct = default)
    {
        using var resp = await SendWithAuthAsync(
            () => new HttpRequestMessage(HttpMethod.Get, "/api/registrar/config"), ct);
        if (!resp.IsSuccessStatusCode) return null;
        return await resp.Content.ReadAsStringAsync(ct);
    }

    /// <summary>Creates the registrar config (POST /api/registrar/config) — EUDIPLO validates
    /// the credentials immediately against the real OIDC endpoint and throws on failure (400
    /// invalid credentials, 503 OIDC endpoint unreachable).</summary>
    public async Task<string> CreateRegistrarConfigAsync(string json, CancellationToken ct = default)
    {
        using var resp = await SendWithAuthAsync(
            () => new HttpRequestMessage(HttpMethod.Post, "/api/registrar/config")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            }, ct);
        var text = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"EUDIPLO registrar-config create: HTTP {(int)resp.StatusCode} {text}");
        return text;
    }

    /// <summary>Updates the registrar config (PATCH /api/registrar/config, partial update — only
    /// the auth fields you send are re-validated).</summary>
    public async Task<string> UpdateRegistrarConfigAsync(string json, CancellationToken ct = default)
    {
        using var resp = await SendWithAuthAsync(
            () => new HttpRequestMessage(HttpMethod.Patch, "/api/registrar/config")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            }, ct);
        var text = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"EUDIPLO registrar-config update: HTTP {(int)resp.StatusCode} {text}");
        return text;
    }

    /// <summary>Deletes the registrar config (DELETE /api/registrar/config; 404 is ignored).</summary>
    public async Task DeleteRegistrarConfigAsync(CancellationToken ct = default)
    {
        using var resp = await SendWithAuthAsync(
            () => new HttpRequestMessage(HttpMethod.Delete, "/api/registrar/config"), ct);
        if (!resp.IsSuccessStatusCode && resp.StatusCode != HttpStatusCode.NotFound)
            throw new InvalidOperationException($"EUDIPLO registrar-config delete: HTTP {(int)resp.StatusCode}");
    }

    /// <summary>Automatically obtains an access certificate from the registrar for the given
    /// key-chain (POST /api/registrar/access-certificate) — EUDIPLO authenticates itself against
    /// the registrar (using the tenant's registrar config), retrieves the cert, and attaches it
    /// directly to the key-chain. Requires a relying party already registered with the
    /// registrar. Returns the registrar-assigned cert id and its PEM.
    /// Note: the field name per <c>CreateAccessCertificateDto.schema.json</c> is <c>keyId</c> —
    /// the prose docs (getting-started/registrar.md) incorrectly call it <c>keyChainId</c>; the
    /// generated schema is the more reliable source.</summary>
    public async Task<(string Id, string CertPem)> CreateAccessCertificateViaRegistrarAsync(string keyChainId, CancellationToken ct = default)
    {
        var body = JsonSerializer.Serialize(new { keyId = keyChainId });
        using var resp = await SendWithAuthAsync(
            () => new HttpRequestMessage(HttpMethod.Post, "/api/registrar/access-certificate")
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            }, ct);
        var text = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"EUDIPLO registrar access-certificate: HTTP {(int)resp.StatusCode} {text}");

        using var doc = JsonDocument.Parse(text);
        var root = doc.RootElement;
        if (!root.TryGetProperty("id", out var i) || i.GetString() is not { } id
            || !root.TryGetProperty("crt", out var c) || c.GetString() is not { } crt)
            throw new InvalidOperationException("EUDIPLO registrar access-certificate response had no id/crt");
        return (id, crt);
    }
}
