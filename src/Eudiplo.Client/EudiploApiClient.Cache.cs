namespace Eudiplo.Client;

/// <summary>
/// Administrative control over EUDIPLO's internal trust-list/status-list caches — mostly
/// useful for diagnosing or forcing a refresh of externally-resolved trust material without
/// waiting for the cache TTL to expire.
/// </summary>
public partial class EudiploApiClient
{
    /// <summary>Reads cache statistics (GET /api/cache/stats).</summary>
    public async Task<string?> GetCacheStatsAsync(CancellationToken ct = default)
    {
        using var resp = await SendWithAuthAsync(
            () => new HttpRequestMessage(HttpMethod.Get, "/api/cache/stats"), ct);
        if (!resp.IsSuccessStatusCode) return null;
        return await resp.Content.ReadAsStringAsync(ct);
    }

    /// <summary>Clears all caches (DELETE /api/cache).</summary>
    public async Task ClearAllCachesAsync(CancellationToken ct = default)
    {
        using var resp = await SendWithAuthAsync(
            () => new HttpRequestMessage(HttpMethod.Delete, "/api/cache"), ct);
        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"EUDIPLO cache clear: HTTP {(int)resp.StatusCode}");
    }

    /// <summary>Clears only the trust-list cache (DELETE /api/cache/trust-list).</summary>
    public async Task ClearTrustListCacheAsync(CancellationToken ct = default)
    {
        using var resp = await SendWithAuthAsync(
            () => new HttpRequestMessage(HttpMethod.Delete, "/api/cache/trust-list"), ct);
        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"EUDIPLO trust-list cache clear: HTTP {(int)resp.StatusCode}");
    }

    /// <summary>Clears only the status-list cache (DELETE /api/cache/status-list).</summary>
    public async Task ClearStatusListCacheAsync(CancellationToken ct = default)
    {
        using var resp = await SendWithAuthAsync(
            () => new HttpRequestMessage(HttpMethod.Delete, "/api/cache/status-list"), ct);
        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"EUDIPLO status-list cache clear: HTTP {(int)resp.StatusCode}");
    }
}
