using System.Runtime.CompilerServices;

namespace Eudiplo.Client;

/// <summary>
/// Session listing/management beyond the single-session polling in
/// <c>EudiploApiClient.Presentation.cs</c> (<see cref="GetSessionAsync"/>) — listing all
/// sessions, deleting one, and reading its log entries.
/// </summary>
public partial class EudiploApiClient
{
    /// <summary>Lists sessions for the tenant, paginated (GET /api/session).
    /// <paramref name="query"/> is an optional raw query string (e.g. "page=2&amp;limit=50",
    /// with or without a leading "?") — see the EUDIPLO API reference for supported
    /// filter/pagination parameters.</summary>
    public async Task<IReadOnlyList<JsonElement>> GetSessionsAsync(string? query = null, CancellationToken ct = default)
    {
        var path = string.IsNullOrEmpty(query) ? "/api/session"
            : query.StartsWith('?') ? $"/api/session{query}" : $"/api/session?{query}";
        using var resp = await SendWithAuthAsync(
            () => new HttpRequestMessage(HttpMethod.Get, path), ct);
        if (!resp.IsSuccessStatusCode) return Array.Empty<JsonElement>();
        return ParseJsonArray(await resp.Content.ReadAsStringAsync(ct));
    }

    /// <summary>Deletes a session (DELETE /api/session/{id}).</summary>
    public async Task DeleteSessionAsync(string id, CancellationToken ct = default)
    {
        using var resp = await SendWithAuthAsync(
            () => new HttpRequestMessage(HttpMethod.Delete, $"/api/session/{id}"), ct);
        var text = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"EUDIPLO session delete: HTTP {(int)resp.StatusCode} {text}");
    }

    /// <summary>Reads the log entries recorded for a session (GET /api/session/{id}/logs) —
    /// useful for diagnosing a failed issuance/presentation flow.</summary>
    public async Task<IReadOnlyList<JsonElement>> GetSessionLogsAsync(string id, CancellationToken ct = default)
    {
        using var resp = await SendWithAuthAsync(
            () => new HttpRequestMessage(HttpMethod.Get, $"/api/session/{id}/logs"), ct);
        if (!resp.IsSuccessStatusCode) return Array.Empty<JsonElement>();
        return ParseJsonArray(await resp.Content.ReadAsStringAsync(ct));
    }

    /// <summary>
    /// Subscribes to live status updates for a session via Server-Sent Events
    /// (GET /api/session/{id}/events). Unlike every other method in this client, this bypasses
    /// <c>SendWithAuthAsync</c>'s buffered send-and-retry: SSE is a long-lived streaming
    /// connection, so the request is sent with <see cref="HttpCompletionOption.ResponseHeadersRead"/>
    /// and read incrementally instead. There is no automatic 401-retry here — if the access
    /// token expires mid-stream, the enumeration throws and the caller should re-subscribe.
    /// Yields the raw payload of each "data:" line as it arrives; enumeration ends when the
    /// server closes the connection or <paramref name="ct"/> is cancelled.
    ///
    /// Unlike every other endpoint, this one does NOT accept the token via the
    /// <c>Authorization</c> header — EUDIPLO's SSE controller only checks a <c>token</c> query
    /// parameter (documented reason: browsers' <c>EventSource</c> API can't send custom
    /// headers, so this had to be made query-string-based to be directly usable from a
    /// frontend). Sending it as a header instead gets a 401.
    ///
    /// Also unlike every other endpoint, this bypasses <see cref="SendWithAuthAsync"/>'s
    /// per-call timeout entirely and calls <c>_http</c> directly with only the caller's
    /// <paramref name="ct"/> — an SSE subscription is expected to run far longer than a
    /// normal request/response round trip (waiting on a human to unlock their wallet, pick
    /// a credential, and confirm disclosure easily takes longer than a few seconds), so
    /// applying the same short per-call timeout used everywhere else would abort the stream
    /// on nearly every real use before anything interesting happens.
    /// </summary>
    public async IAsyncEnumerable<string> SubscribeToSessionEventsAsync(string id, [EnumeratorCancellation] CancellationToken ct = default)
    {
        // Fetching the token itself is a normal short request — bound it like everywhere
        // else. Only the streaming read below is deliberately left unbounded.
        string token;
        using (var tokenCts = CancellationTokenSource.CreateLinkedTokenSource(ct))
        {
            tokenCts.CancelAfter(_requestTimeout);
            token = await GetTokenAsync(tokenCts.Token);
        }
        using var req = new HttpRequestMessage(HttpMethod.Get, $"/api/session/{id}/events?token={Uri.EscapeDataString(token)}");

        using var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"EUDIPLO session events: HTTP {(int)resp.StatusCode}");

        using var stream = await resp.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);
        // Loop on ReadLineAsync's null-at-end-of-stream instead of checking
        // reader.EndOfStream first — that property can block synchronously on I/O, which
        // defeats the point of an async streaming read (CA2024).
        while (true)
        {
            var line = await reader.ReadLineAsync(ct);
            if (line is null) yield break;
            if (line.StartsWith("data:", StringComparison.Ordinal))
                yield return line[5..].TrimStart();
        }
    }
}
