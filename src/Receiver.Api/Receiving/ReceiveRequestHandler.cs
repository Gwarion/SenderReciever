using System.Buffers;
using System.Diagnostics;
using System.IO.Compression;

namespace Receiver.Api;

public sealed class ReceiveRequestHandler(
    IConfiguration configuration,
    ILogger<ReceiveRequestHandler> logger)
{
    /// <summary>
    /// Receives a raw or gzip-compressed request body and streams it to disk.
    /// </summary>
    /// <param name="context">Current HTTP request context.</param>
    /// <param name="cancellationToken">Token used to cancel receive processing.</param>
    /// <returns>Receive result including output file, byte count, line count, and memory metrics.</returns>
    public async Task<IResult> ReceiveAsync(HttpContext context, CancellationToken cancellationToken)
    {
        var options = ReceiveOptions.From(context.Request.Query, configuration);
        var isGzip = context.Request.Headers.ContentEncoding.Any(
            value => string.Equals(value, "gzip", StringComparison.OrdinalIgnoreCase));

        Directory.CreateDirectory(options.OutputDirectory);

        var filePath = Path.Combine(options.OutputDirectory, options.OutputFileName);
        var buffer = ArrayPool<byte>.Shared.Rent(options.BufferSizeBytes);
        var started = Stopwatch.StartNew();

        long bytes = 0;
        long lines = 0;
        var linesSinceFlush = 0;
        var flushes = 0;

        logger.LogInformation(
            "Receiving request body into {FilePath}. FlushEveryLines={FlushEveryLines:n0}, BufferSizeBytes={BufferSizeBytes:n0}, Gzip={Gzip}",
            filePath,
            options.FlushEveryLines,
            options.BufferSizeBytes,
            isGzip);

        try
        {
            await using var gzip = isGzip
                ? new GZipStream(context.Request.Body, CompressionMode.Decompress, leaveOpen: true)
                : null;
            var input = gzip ?? context.Request.Body;

            await using var output = new FileStream(
                filePath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.Read,
                bufferSize: options.BufferSizeBytes,
                options: FileOptions.Asynchronous | FileOptions.SequentialScan);

            while (true)
            {
                var read = await input.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
                if (read == 0)
                {
                    break;
                }

                await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                bytes += read;

                for (var i = 0; i < read; i++)
                {
                    if (buffer[i] != (byte)'\n')
                    {
                        continue;
                    }

                    lines++;
                    linesSinceFlush++;
                }

                if (linesSinceFlush < options.FlushEveryLines)
                {
                    continue;
                }

                await output.FlushAsync(cancellationToken);
                flushes++;
                logger.LogInformation(
                    "Receiver flushed file after {Lines:n0} lines and {Bytes:n0} bytes. {Metrics}",
                    lines,
                    bytes,
                    ProcessMetrics.Capture().ToLogString());
                linesSinceFlush = 0;
            }

            await output.FlushAsync(cancellationToken);
            flushes++;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }

        started.Stop();

        return Results.Ok(new
        {
            filePath,
            bytes,
            lines,
            flushes,
            elapsed = started.Elapsed,
            receiver = ProcessMetrics.Capture()
        });
    }
}
