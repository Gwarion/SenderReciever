using System.Diagnostics;
using System.Text;
using MediatR;

namespace Sender.Api;

sealed class StartUploadCommandHandler(
    IConfiguration configuration,
    IDatabaseRepository databaseRepository,
    IUploadRepository uploadRepository,
    UploadLineFormatter lineFormatter,
    ILogger<StartUploadCommandHandler> logger)
    : IRequestHandler<StartUploadCommand, StartUploadResponse>
{
    public async Task<StartUploadResponse> Handle(
        StartUploadCommand request,
        CancellationToken cancellationToken)
    {
        var command = request.Normalize(configuration);
        var progress = new UploadProgress(command);
        var started = Stopwatch.StartNew();

        logger.LogInformation(
            "MediatR upload command started. Receiver={ReceiverUrl}, Range={StartDate:yyyy-MM-dd}..{EndDate:yyyy-MM-dd}, Period={MonthsPerChunk} months, Rows={Rows}",
            command.ReceiverUri,
            command.StartDate,
            command.EndDate,
            command.MonthsPerChunk,
            command.Rows is null ? $"{command.MinRowsPerChunk:n0}-{command.MaxRowsPerChunk:n0}/period" : $"{command.Rows:n0} total");

        var repositoryResult = await uploadRepository.UploadGzipAsync(
            command,
            (stream, ct) => WriteFormattedDataAsync(command, progress, stream, ct),
            cancellationToken);

        started.Stop();

        return new(
            progress.ToSummary(started.Elapsed),
            repositoryResult,
            ProcessMetrics.Capture());
    }

    async Task WriteFormattedDataAsync(
        StartUploadCommand command,
        UploadProgress progress,
        Stream destination,
        CancellationToken cancellationToken)
    {
        var linesSinceFlush = 0;
        await using var writer = new StreamWriter(destination, new UTF8Encoding(false), bufferSize: 16 * 1024, leaveOpen: true)
        {
            NewLine = "\n"
        };

        if (command.Rows is { } rows)
        {
            var records = await databaseRepository.GetAllItemsAsync(
                command.StartDate!.Value,
                command.EndDate!.Value,
                checked((int)rows),
                checked((int)rows),
                cancellationToken);

            await WriteRecordsAsync(records, writer, progress, command, cancellationToken);
            progress.RecordPeriod(command.StartDate.Value, command.EndDate.Value, records.Count);
            await writer.FlushAsync(cancellationToken);
            return;
        }

        for (var from = command.StartDate!.Value; from < command.EndDate!.Value; from = from.AddMonths(command.MonthsPerChunk))
        {
            var to = Min(from.AddMonths(command.MonthsPerChunk), command.EndDate.Value);
            var records = await databaseRepository.GetAllItemsAsync(
                from,
                to,
                command.MinRowsPerChunk,
                command.MaxRowsPerChunk,
                cancellationToken);

            logger.LogInformation(
                "Fetched simulated DB period {From:yyyy-MM-dd}..{To:yyyy-MM-dd}: {Rows:n0} records",
                from,
                to,
                records.Count);

            await WriteRecordsAsync(records, writer, progress, command, cancellationToken);
            progress.RecordPeriod(from, to, records.Count);
        }

        await writer.FlushAsync(cancellationToken);

        async Task WriteRecordsAsync(
            List<DatabaseRecord> records,
            StreamWriter streamWriter,
            UploadProgress uploadProgress,
            StartUploadCommand uploadCommand,
            CancellationToken ct)
        {
            foreach (var record in records)
            {
                ct.ThrowIfCancellationRequested();
                await streamWriter.WriteLineAsync(lineFormatter.FormatLine(record).AsMemory(), ct);

                uploadProgress.RecordRow();
                linesSinceFlush++;

                if (linesSinceFlush < uploadCommand.FlushEveryLines)
                {
                    continue;
                }

                await streamWriter.FlushAsync(ct);
                logger.LogInformation(
                    "Handler flushed after formatting {Rows:n0} records and {Bytes:n0} bytes. {Metrics}",
                    uploadProgress.RowsWritten,
                    uploadProgress.BytesWritten,
                    ProcessMetrics.Capture().ToLogString());
                linesSinceFlush = 0;
            }
        }
    }

    static DateOnly Min(DateOnly left, DateOnly right) => left < right ? left : right;
}
