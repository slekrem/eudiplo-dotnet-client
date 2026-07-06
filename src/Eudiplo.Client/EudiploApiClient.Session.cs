using System.Text.Json;

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
}
