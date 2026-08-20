using System.IO.Compression;
using System.Net;

namespace Sender.Api;

sealed class GzipStreamingUploadHttpContent : HttpContent
{
    readonly CountingWriteStream countingStream = new();
    readonly Func<Stream, CancellationToken, Task> writePayloadAsync;

    public GzipStreamingUploadHttpContent(Func<Stream, CancellationToken, Task> writePayloadAsync)
    {
        this.writePayloadAsync = writePayloadAsync;
        Headers.ContentEncoding.Add("gzip");
    }

    public long CompressedBytesWritten => countingStream.BytesWritten;

    protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) =>
        SerializeToStreamAsync(stream, context, CancellationToken.None);

    protected override async Task SerializeToStreamAsync(
        Stream stream,
        TransportContext? context,
        CancellationToken cancellationToken)
    {
        countingStream.Reset(stream);

        await using var gzip = new GZipStream(countingStream, CompressionLevel.Fastest, leaveOpen: true);
        await writePayloadAsync(gzip, cancellationToken);
    }

    protected override bool TryComputeLength(out long length)
    {
        length = 0;
        return false;
    }
}
