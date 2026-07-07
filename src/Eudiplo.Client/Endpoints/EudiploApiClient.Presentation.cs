namespace Eudiplo.Client;

/// <summary>
/// Presentation offers + session polling (relying-party-side OpenID4VP flows). Three calls
/// make up the core flow: OAuth2 token (base file), create a presentation offer, poll the
/// session status. The DCQL query itself lives server-side as a presentation config.
/// </summary>
public partial class EudiploApiClient
{
    /// <summary>
    /// Creates a presentation session. Returns the <c>openid4vp://</c> URI (for a QR code) and
    /// the EUDIPLO session id (for polling).
    /// </summary>
    public async Task<(string RequestUrl, string SessionId)> CreateOfferAsync(
        string presentationConfigId, string? redirectUri = null, CancellationToken ct = default)
    {
        // redirectUri (optional): after a successful presentation, EUDIPLO redirects the wallet
        // to `redirectUri?response_code=…` (OID4VP §13.3) — used for the dynamic-issuance flow,
        // where the wallet needs to return to an issuer callback.
        var body = redirectUri is null
            ? JsonSerializer.Serialize(new { response_type = "uri", requestId = presentationConfigId })
            : JsonSerializer.Serialize(new { response_type = "uri", requestId = presentationConfigId, redirectUri });
        using var resp = await SendWithAuthAsync(
            () => new HttpRequestMessage(HttpMethod.Post, "/api/verifier/offer")
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            }, ct);
        resp.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(ct));
        var root = doc.RootElement;
        if (!root.TryGetProperty("uri", out var uri) || uri.GetString() is not { } requestUrl
            || !root.TryGetProperty("session", out var sid) || sid.GetString() is not { } sessionId)
            throw new InvalidOperationException("EUDIPLO offer response had no uri/session");

        return (requestUrl, sessionId);
    }

    /// <summary>
    /// Polls a session. Returns the JSON body, or null on a network/HTTP error.
    /// </summary>
    public async Task<string?> GetSessionAsync(string sessionId, CancellationToken ct = default)
    {
        using var resp = await SendWithAuthAsync(
            () => new HttpRequestMessage(HttpMethod.Get, $"/api/session/{sessionId}"), ct);
        if (!resp.IsSuccessStatusCode) return null;
        return await resp.Content.ReadAsStringAsync(ct);
    }
}
