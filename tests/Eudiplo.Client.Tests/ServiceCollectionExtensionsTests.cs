using Microsoft.Extensions.DependencyInjection;

namespace Eudiplo.Client.Tests;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddEudiploClient_WithAction_RegistersResolvableClient()
    {
        var services = new ServiceCollection();
        services.AddEudiploClient(o =>
        {
            o.BaseUrl = "https://eudiplo.example.com";
            o.ClientId = "my-client";
            o.ClientSecret = "my-secret";
        });

        using var provider = services.BuildServiceProvider();

        var client = provider.GetRequiredService<EudiploApiClient>();
        Assert.NotNull(client);
    }

    [Fact]
    public void AddEudiploClient_ConfiguresNamedHttpClientBaseAddressAndInfiniteTimeout()
    {
        var services = new ServiceCollection();
        services.AddEudiploClient(o =>
        {
            o.BaseUrl = "https://eudiplo.example.com";
            o.HttpTimeoutSeconds = 42;
        });

        using var provider = services.BuildServiceProvider();
        var http = provider.GetRequiredService<IHttpClientFactory>().CreateClient(EudiploApiClient.HttpClientName);

        Assert.Equal(new Uri("https://eudiplo.example.com"), http.BaseAddress);
        // Deliberately infinite regardless of HttpTimeoutSeconds — EudiploApiClient enforces
        // that per call itself instead, so SubscribeToSessionEventsAsync's long-lived stream
        // reads can be exempted from it (see EudiploApiClient's class doc comment).
        Assert.Equal(Timeout.InfiniteTimeSpan, http.Timeout);
    }

    [Fact]
    public void AddEudiploClient_ResolvingClientTwice_ReturnsSameSingletonInstance()
    {
        var services = new ServiceCollection();
        services.AddEudiploClient(o => o.BaseUrl = "https://eudiplo.example.com");
        using var provider = services.BuildServiceProvider();

        var client1 = provider.GetRequiredService<EudiploApiClient>();
        var client2 = provider.GetRequiredService<EudiploApiClient>();

        Assert.Same(client1, client2);
    }

    [Fact]
    public void AddEudiploClient_WithoutBaseUrl_LeavesHttpClientBaseAddressNull()
    {
        var services = new ServiceCollection();
        services.AddEudiploClient(_ => { });

        using var provider = services.BuildServiceProvider();
        var http = provider.GetRequiredService<IHttpClientFactory>().CreateClient(EudiploApiClient.HttpClientName);

        Assert.Null(http.BaseAddress);
    }
}
