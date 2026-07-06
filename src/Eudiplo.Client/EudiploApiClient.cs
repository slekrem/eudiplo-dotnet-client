using System.Net;
using System.Text;
using System.Text.Json;

namespace Eudiplo.Client;

/// <summary>
/// HTTP client for a self-hosted EUDIPLO instance
/// (<see href="https://github.com/openwallet-foundation-labs/eudiplo"/>) — the OpenWallet
/// Foundation's middleware for EUDI Wallet issuance, presentation, and relying-party
/// administration.
///
/// This file only contains OAuth2 client-credentials token handling and the authenticated
/// send helper. The actual EUDIPLO API areas are implemented as <c>partial class</c> members
/// in their own files (<c>EudiploApiClient.Presentation.cs</c>, <c>.VerifierConfig.cs</c>,
/// <c>.TrustList.cs</c>, <c>.KeyChain.cs</c>, <c>.SchemaMetadata.cs</c>, <c>.Registrar.cs</c>,
/// <c>.Issuer.cs</c>, <c>.Client.cs</c>, <c>.Tenant.cs</c>) — one object, one <see cref="HttpClient"/>,
/// one token cache per relying party, but navigable by EUDIPLO API area.
/// </summary>
public partial class EudiploApiClient(HttpClient http, string clientId, string clientSecret)
{
    /// <summary>Name to use when registering the named <see cref="HttpClient"/> via
    /// <see cref="IHttpClientFactory"/> (see <c>AddEudiploClient</c>).</summary>
    public const string HttpClientName = "EudiploClient";

    private readonly HttpClient _http   = http;
    private readonly string _clientId   = clientId;
    private readonly string _clientSecret = clientSecret;

    private string?   _token;
    private DateTime  _tokenExpiresAt = DateTime.MinValue;
    private readonly SemaphoreSlim _tokenLock = new(1, 1);

    private async Task<string> GetTokenAsync(CancellationToken ct)
    {
        if (_token is not null && DateTime.UtcNow < _tokenExpiresAt)
            return _token;

        await _tokenLock.WaitAsync(ct);
        try
        {
            if (_token is not null && DateTime.UtcNow < _tokenExpiresAt)
                return _token;

            var body = JsonSerializer.Serialize(new
            {
                grant_type    = "client_credentials",
                client_id     = _clientId,
                client_secret = _clientSecret,
            });
            using var content = new StringContent(body, Encoding.UTF8, "application/json");
            var resp = await _http.PostAsync("/api/oauth2/token", content, ct);
            resp.EnsureSuccessStatusCode();

            using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(ct));
            _token = doc.RootElement.GetProperty("access_token").GetString()
                ?? throw new InvalidOperationException("EUDIPLO did not return an access_token");
            // Token TTL is 24h server-side; cache conservatively for 30 minutes.
            _tokenExpiresAt = DateTime.UtcNow.AddMinutes(30);
            return _token;
        }
        finally { _tokenLock.Release(); }
    }

    private void InvalidateToken()
    {
        _token = null;
        _tokenExpiresAt = DateTime.MinValue;
    }

    /// <summary>
    /// Sends an authenticated request. On 401, the cached token is discarded once, refetched
    /// (e.g. after secret rotation) and the request is retried.
    /// <paramref name="build"/> must construct a fresh request on every call — an
    /// <see cref="HttpRequestMessage"/> (and its content) cannot be reused after being sent.
    /// </summary>
    private async Task<HttpResponseMessage> SendWithAuthAsync(Func<HttpRequestMessage> build, CancellationToken ct)
    {
        var token = await GetTokenAsync(ct);
        var req   = build();
        req.Headers.Authorization = new("Bearer", token);
        var resp = await _http.SendAsync(req, ct);

        if (resp.StatusCode == HttpStatusCode.Unauthorized)
        {
            resp.Dispose();
            InvalidateToken();
            token = await GetTokenAsync(ct);
            req   = build();
            req.Headers.Authorization = new("Bearer", token);
            resp = await _http.SendAsync(req, ct);
        }
        return resp;
    }
}
