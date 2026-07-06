namespace Eudiplo.Client;

/// <summary>Configuration for talking to a self-hosted EUDIPLO instance.</summary>
public class EudiploClientOptions
{
    /// <summary>Default configuration section name used by the <c>IConfiguration</c>-based
    /// overload of <c>AddEudiploClient</c>.</summary>
    public const string SectionName = "Eudiplo";

    /// <summary>Base URL of your EUDIPLO instance (e.g. <c>https://eudiplo.example.com</c>).</summary>
    public string? BaseUrl { get; set; }

    /// <summary>HTTP request timeout in seconds. Default: 15.</summary>
    public int HttpTimeoutSeconds { get; set; } = 15;

    /// <summary>
    /// OAuth2 client-credentials id/secret used to construct the injectable
    /// <see cref="EudiploApiClient"/> singleton. Leave both null if you only need multi-tenant
    /// scenarios and construct <see cref="EudiploApiClient"/> instances yourself per tenant
    /// (e.g. using a root client with the <c>tenants:manage</c> role, or a per-tenant client
    /// resolved from your own storage).
    /// </summary>
    public string? ClientId { get; set; }

    /// <inheritdoc cref="ClientId"/>
    public string? ClientSecret { get; set; }
}
