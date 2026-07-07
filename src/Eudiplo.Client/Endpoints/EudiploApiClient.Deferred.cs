namespace Eudiplo.Client;

/// <summary>
/// Deferred-credential issuance — completing or failing a credential issuance that was
/// deferred at offer time (OID4VCI deferred flow), e.g. because a claim value wasn't available
/// yet and needed asynchronous processing.
/// </summary>
public partial class EudiploApiClient
{
    /// <summary>Completes a deferred issuance transaction with the now-available claims
    /// (POST /api/issuer/deferred/{transactionId}/complete).</summary>
    public async Task<string> CompleteDeferredAsync(string transactionId, string json, CancellationToken ct = default)
    {
        using var resp = await SendWithAuthAsync(
            () => new HttpRequestMessage(HttpMethod.Post, $"/api/issuer/deferred/{transactionId}/complete")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            }, ct);
        var text = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"EUDIPLO deferred complete: HTTP {(int)resp.StatusCode} {text}");
        return text;
    }

    /// <summary>Fails a deferred issuance transaction (POST /api/issuer/deferred/{transactionId}/fail)
    /// — the wallet's next deferred-credential poll will receive an error instead of hanging.
    /// <paramref name="json"/> may be null if EUDIPLO doesn't require a reason body.</summary>
    public async Task FailDeferredAsync(string transactionId, string? json = null, CancellationToken ct = default)
    {
        using var resp = await SendWithAuthAsync(
            () => new HttpRequestMessage(HttpMethod.Post, $"/api/issuer/deferred/{transactionId}/fail")
            {
                Content = json is null ? null : new StringContent(json, Encoding.UTF8, "application/json"),
            }, ct);
        var text = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"EUDIPLO deferred fail: HTTP {(int)resp.StatusCode} {text}");
    }
}
