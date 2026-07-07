namespace Eudiplo.Client;

/// <summary>Generic file storage — used e.g. to host an image referenced by a credential's
/// display metadata (logo, background) at a stable EUDIPLO-hosted URL.</summary>
public partial class EudiploApiClient
{
    /// <summary>Uploads a file (POST /api/storage, multipart/form-data). Returns the raw JSON
    /// response (typically containing the storage key and/or a public URL).</summary>
    public async Task<string> UploadAsync(Stream content, string fileName, string contentType, CancellationToken ct = default)
    {
        // A fresh MultipartFormDataContent/StreamContent must be built inside the retry
        // delegate — SendWithAuthAsync may call build() twice (once more after a 401), and an
        // HttpContent cannot be sent more than once.
        using var resp = await SendWithAuthAsync(() =>
        {
            // Reset position so a 401-triggered retry re-sends the same bytes instead of an
            // empty body (only possible for seekable streams — pass a MemoryStream/FileStream).
            if (content.CanSeek) content.Position = 0;
            var streamContent = new StreamContent(content);
            streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
            var form = new MultipartFormDataContent { { streamContent, "file", fileName } };
            return new HttpRequestMessage(HttpMethod.Post, "/api/storage") { Content = form };
        }, ct);
        var text = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"EUDIPLO storage upload: HTTP {(int)resp.StatusCode} {text}");
        return text;
    }

    /// <summary>Downloads a previously uploaded file by its storage key (GET /api/storage/{key}).
    /// This endpoint is unauthenticated on EUDIPLO's side, but is still sent through the
    /// authenticated client for consistency; returns null if not found. Returns the raw bytes —
    /// the caller is responsible for interpreting the content type.</summary>
    public async Task<byte[]?> DownloadAsync(string key, CancellationToken ct = default)
    {
        using var resp = await SendWithAuthAsync(
            () => new HttpRequestMessage(HttpMethod.Get, $"/api/storage/{key}"), ct);
        if (!resp.IsSuccessStatusCode) return null;
        return await resp.Content.ReadAsByteArrayAsync(ct);
    }
}
