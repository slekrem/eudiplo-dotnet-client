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
///
/// Implements <see cref="IDisposable"/> only to release the internal token-refresh lock —
/// the <see cref="HttpClient"/> passed to the constructor is owned by the caller (typically
/// <see cref="IHttpClientFactory"/>) and is never disposed here.
///
/// The <see cref="HttpClient"/> itself is expected to have an effectively infinite
/// <see cref="HttpClient.Timeout"/> (this is how <c>AddEudiploClient</c> configures the named
/// client) — every regular request is instead bounded by <paramref name="requestTimeout"/>,
/// applied per call in <see cref="SendWithAuthAsync"/>. <see cref="SubscribeToSessionEventsAsync"/>
/// deliberately does not go through that helper and so is unaffected by it, since an SSE
/// subscription is expected to run far longer than a normal request/response round trip.
/// </summary>
public partial class EudiploApiClient(HttpClient http, string clientId, string clientSecret, TimeSpan? requestTimeout = null) : IDisposable
{
    /// <summary>Name to use when registering the named <see cref="HttpClient"/> via
    /// <see cref="IHttpClientFactory"/> (see <c>AddEudiploClient</c>).</summary>
    public const string HttpClientName = "EudiploClient";

    private readonly HttpClient _http = http;
    private readonly string _clientId = clientId;
    private readonly string _clientSecret = clientSecret;

    /// <summary>Per-call timeout applied in <see cref="SendWithAuthAsync"/>. Default matches
    /// <see cref="EudiploClientOptions.HttpTimeoutSeconds"/>'s own default (15s) for
    /// consumers who construct <see cref="EudiploApiClient"/> directly instead of through
    /// <c>AddEudiploClient</c> (e.g. this library's own multi-tenant samples).</summary>
    private readonly TimeSpan _requestTimeout = requestTimeout ?? TimeSpan.FromSeconds(15);

    private string? _token;
    private DateTime _tokenExpiresAt = DateTime.MinValue;
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
                grant_type = "client_credentials",
                client_id = _clientId,
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
    ///
    /// Applies <see cref="_requestTimeout"/> via a linked <see cref="CancellationTokenSource"/>
    /// rather than relying on <see cref="HttpClient.Timeout"/> — the latter is expected to be
    /// infinite on <see cref="_http"/> (see the class doc comment), since it would otherwise
    /// also bound <see cref="SubscribeToSessionEventsAsync"/>'s long-lived stream reads, which
    /// don't go through this helper.
    /// </summary>
    private async Task<HttpResponseMessage> SendWithAuthAsync(Func<HttpRequestMessage> build, CancellationToken ct)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(_requestTimeout);
        var timeoutCt = cts.Token;

        var token = await GetTokenAsync(timeoutCt);
        var req = build();
        req.Headers.Authorization = new("Bearer", token);
        var resp = await _http.SendAsync(req, timeoutCt);

        if (resp.StatusCode == HttpStatusCode.Unauthorized)
        {
            resp.Dispose();
            InvalidateToken();
            token = await GetTokenAsync(timeoutCt);
            req = build();
            req.Headers.Authorization = new("Bearer", token);
            resp = await _http.SendAsync(req, timeoutCt);
        }
        return resp;
    }

    /// <summary>Parses a EUDIPLO list response into a flat array of elements — EUDIPLO list
    /// endpoints return either a bare JSON array or a <c>{"items": [...]}</c> wrapper depending
    /// on the endpoint; this normalizes both shapes for every <c>GetXxxAsync</c> list method.</summary>
    private static IReadOnlyList<JsonElement> ParseJsonArray(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var arr = root.ValueKind == JsonValueKind.Array ? root
            : root.TryGetProperty("items", out var it) ? it : default;
        if (arr.ValueKind != JsonValueKind.Array) return Array.Empty<JsonElement>();
        var list = new List<JsonElement>();
        foreach (var e in arr.EnumerateArray()) list.Add(e.Clone());
        return list;
    }

    /// <summary>Releases the internal token-refresh lock. Does not dispose the
    /// <see cref="HttpClient"/> passed to the constructor — that remains the caller's
    /// responsibility.</summary>
    public void Dispose()
    {
        _tokenLock.Dispose();
        GC.SuppressFinalize(this);
    }
}
