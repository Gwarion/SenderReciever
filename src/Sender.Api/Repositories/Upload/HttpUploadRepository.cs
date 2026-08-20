namespace Sender.Api;

sealed class HttpUploadRepository(HttpClient httpClient) : IUploadRepository
{
    public async Task<RepositoryUploadResult> UploadGzipAsync(
        StartUploadCommand command,
        Func<Stream, CancellationToken, Task> writePayloadAsync,
        CancellationToken cancellationToken)
    {
        using var content = new GzipStreamingUploadHttpContent(writePayloadAsync);
        using var request = new HttpRequestMessage(HttpMethod.Post, command.ReceiverUri) { Content = content };
        using var response = await httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        var receiverBody = await response.Content.ReadAsStringAsync(cancellationToken);
        response.EnsureSuccessStatusCode();

        return new(response.StatusCode, receiverBody, content.CompressedBytesWritten);
    }
}
