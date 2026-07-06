using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Eudiplo.Client;

/// <summary>Dependency-injection extensions for registering <see cref="EudiploApiClient"/>.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Binds <see cref="EudiploClientOptions"/> from the <c>"Eudiplo"</c> configuration
    /// section, registers the named <see cref="HttpClient"/> (<see cref="EudiploApiClient.HttpClientName"/>),
    /// and — if <see cref="EudiploClientOptions.ClientId"/>/<see cref="EudiploClientOptions.ClientSecret"/>
    /// are configured — an injectable <see cref="EudiploApiClient"/> singleton for that single
    /// tenant/client.</summary>
    public static IServiceCollection AddEudiploClient(this IServiceCollection services, IConfiguration configuration)
        => services.AddEudiploClient(configuration.GetSection(EudiploClientOptions.SectionName));

    /// <inheritdoc cref="AddEudiploClient(IServiceCollection, IConfiguration)"/>
    public static IServiceCollection AddEudiploClient(this IServiceCollection services, IConfigurationSection section)
    {
        services.AddOptions<EudiploClientOptions>().Bind(section);
        return services.AddEudiploClientCore();
    }

    /// <inheritdoc cref="AddEudiploClient(IServiceCollection, IConfiguration)"/>
    public static IServiceCollection AddEudiploClient(this IServiceCollection services, Action<EudiploClientOptions> configure)
    {
        services.Configure(configure);
        return services.AddEudiploClientCore();
    }

    private static IServiceCollection AddEudiploClientCore(this IServiceCollection services)
    {
        services.AddHttpClient(EudiploApiClient.HttpClientName, (sp, c) =>
        {
            var opts = sp.GetRequiredService<IOptions<EudiploClientOptions>>().Value;
            if (!string.IsNullOrWhiteSpace(opts.BaseUrl))
                c.BaseAddress = new Uri(opts.BaseUrl);
            c.Timeout = TimeSpan.FromSeconds(opts.HttpTimeoutSeconds);
        });

        // Only register an injectable client if single-tenant credentials were configured.
        // Multi-tenant consumers (e.g. a root client managing several relying parties) should
        // construct their own EudiploApiClient instances per tenant via IHttpClientFactory
        // instead — the constructor is public precisely for that case.
        services.AddSingleton(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<EudiploClientOptions>>().Value;
            var http = sp.GetRequiredService<IHttpClientFactory>().CreateClient(EudiploApiClient.HttpClientName);
            return new EudiploApiClient(http, opts.ClientId ?? "", opts.ClientSecret ?? "");
        });

        return services;
    }
}
