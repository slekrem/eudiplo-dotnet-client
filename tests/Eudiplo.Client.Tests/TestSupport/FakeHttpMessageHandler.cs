using System.Text;

namespace Eudiplo.Client.Tests.TestSupport;

public sealed record CapturedRequest(HttpMethod Method, Uri? RequestUri, string? Body);

/// <summary>
/// Queue-based fake <see cref="HttpMessageHandler"/> for testing <see cref="EudiploApiClient"/>
/// without a real EUDIPLO server. Responses are dequeued in the order calls are enqueued; every
/// request is captured (method, URI, body) for assertions.
/// </summary>
public sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    public List<CapturedRequest> Requests { get; } = new();

    private readonly Queue<Func<CapturedRequest, CancellationToken, Task<HttpResponseMessage>>> _responses = new();

    public FakeHttpMessageHandler Enqueue(HttpStatusCode status, string? json = null)
        => Enqueue(_ => new HttpResponseMessage(status)
        {
            Content = json is null ? null : new StringContent(json, Encoding.UTF8, "application/json"),
        });

    public FakeHttpMessageHandler Enqueue(Func<CapturedRequest, HttpResponseMessage> respond)
        => Enqueue((req, _) => Task.FromResult(respond(req)));

    public FakeHttpMessageHandler Enqueue(Func<CapturedRequest, CancellationToken, Task<HttpResponseMessage>> respond)
    {
        _responses.Enqueue(respond);
        return this;
    }

    /// <summary>Enqueues a response that isn't returned until <paramref name="delay"/> has
    /// elapsed (honoring cancellation) — for exercising per-call timeout behavior.</summary>
    public FakeHttpMessageHandler EnqueueDelayed(TimeSpan delay, HttpStatusCode status, string? json = null)
        => Enqueue(async (_, ct) =>
        {
            await Task.Delay(delay, ct);
            return new HttpResponseMessage(status)
            {
                Content = json is null ? null : new StringContent(json, Encoding.UTF8, "application/json"),
            };
        });

    /// <summary>Convenience for the OAuth2 token exchange every authenticated call makes first.</summary>
    public FakeHttpMessageHandler EnqueueToken(string accessToken = "test-token")
        => Enqueue(HttpStatusCode.OK, $$"""{"access_token":"{{accessToken}}"}""");

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
        var captured = new CapturedRequest(request.Method, request.RequestUri, body);
        Requests.Add(captured);

        if (_responses.Count == 0)
            throw new InvalidOperationException($"No queued response left for {captured.Method} {captured.RequestUri}.");
        return await _responses.Dequeue()(captured, cancellationToken);
    }
}
