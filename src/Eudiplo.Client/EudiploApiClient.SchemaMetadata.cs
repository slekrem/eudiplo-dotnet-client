using System.Text;

namespace Eudiplo.Client;

/// <summary>Schema metadata signing (for publishing a custom attestation schema with a
/// registrar).</summary>
public partial class EudiploApiClient
{
    /// <summary>Signs and submits schema metadata (POST /api/schema-metadata/sign). Returns the
    /// response body (registrar-assigned id etc.) or throws on HTTP failure.</summary>
    public async Task<string> SignSchemaMetadataAsync(string json, CancellationToken ct = default)
    {
        using var resp = await SendWithAuthAsync(
            () => new HttpRequestMessage(HttpMethod.Post, "/api/schema-metadata/sign")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            }, ct);
        var text = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"EUDIPLO schema-metadata/sign: HTTP {(int)resp.StatusCode} {text}");
        return text;
    }
}
