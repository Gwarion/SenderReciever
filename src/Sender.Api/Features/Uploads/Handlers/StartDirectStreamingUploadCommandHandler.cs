using System.Diagnostics;
using System.Text;
using MediatR;

namespace Sender.Api;

sealed class StartDirectStreamingUploadCommandHandler(
    IConfiguration configuration,
    IDatabaseRepository databaseRepository,
    IUploadRepository uploadRepository,
    UploadLineFormatter lineFormatter,
    ILogger<StartDirectStreamingUploadCommandHandler> logger)
    : IRequestHandler<StartDirectStreamingUploadCommand, StartUploadResponse>
{
    public async Task<StartUploadResponse> Handle(
        StartDirectStreamingUploadCommand request,
        CancellationToken cancellationToken)
    {
        var command = request.Normalize(configuration);
        var progress = new UploadProgress(command);
        var started = Stopwatch.StartNew();

        logger.LogInformation(
            "Direct-stream upload command started. Receiver={ReceiverUrl}, Range={StartDate:yyyy-MM-dd}..{EndDate:yyyy-MM-dd}, Period={MonthsPerChunk} months, Rows={Rows}",
            command.ReceiverUri,
            command.StartDate,
            command.EndDate,
            command.MonthsPerChunk,
            command.Rows is null ? $"{command.MinRowsPerChunk:n0}-{command.MaxRowsPerChunk:n0}/period" : $"{command.Rows:n0} total");

        var repositoryResult = await uploadRepository.UploadGzipAsync(
            command,
            (stream, ct) => WriteDirectlyAsync(command, progress, stream, ct),
            cancellationToken);

        started.Stop();

        return new(
            progress.ToSummary(started.Elapsed),
            repositoryResult,
            ProcessMetrics.Capture());
    }

    async Task WriteDirectlyAsync(
        StartUploadCommand command,
        UploadProgress progress,
        Stream destination,
        CancellationToken cancellationToken)
    {
        await using var writer = new StreamWriter(destination, new UTF8Encoding(false), bufferSize: 16 * 1024, leaveOpen: true)
        {
            NewLine = "\r\n"
        };

        await foreach (var chunk in databaseRepository.FetchChunksAsync(command, cancellationToken))
        {
            logger.LogInformation(
                "Fetched simulated DB period {From:yyyy-MM-dd}..{To:yyyy-MM-dd}: {Rows:n0} records",
                chunk.From,
                chunk.To,
                chunk.Rows);

            foreach (var record in chunk.Records)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await writer.WriteLineAsync(lineFormatter.FormatLine(record).AsMemory(), cancellationToken);
                progress.RecordRow();
            }

            progress.RecordPeriod(chunk.From, chunk.To, chunk.Rows);
        }
    }
}
