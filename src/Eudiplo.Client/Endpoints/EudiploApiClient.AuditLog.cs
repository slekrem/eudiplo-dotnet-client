namespace Eudiplo.Client;

/// <summary>Read-only access to EUDIPLO's own audit log for the current tenant.</summary>
public partial class EudiploApiClient
{
    /// <summary>Lists recent audit-log entries for the tenant (GET /api/admin/audit-logs).</summary>
    public async Task<IReadOnlyList<JsonElement>> GetAuditLogAsync(CancellationToken ct = default)
    {
        using var resp = await SendWithAuthAsync(
            () => new HttpRequestMessage(HttpMethod.Get, "/api/admin/audit-logs"), ct);
        if (!resp.IsSuccessStatusCode) return Array.Empty<JsonElement>();
        return ParseJsonArray(await resp.Content.ReadAsStringAsync(ct));
    }
}
