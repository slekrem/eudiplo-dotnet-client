namespace Eudiplo.Client.Tests.TestSupport;

public static class TestClientFactory
{
    public static (EudiploApiClient Client, FakeHttpMessageHandler Handler) Create(
        string clientId = "test-client", string clientSecret = "test-secret")
    {
        var handler = new FakeHttpMessageHandler();
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://eudiplo.test") };
        var client = new EudiploApiClient(http, clientId, clientSecret);
        return (client, handler);
    }
}
