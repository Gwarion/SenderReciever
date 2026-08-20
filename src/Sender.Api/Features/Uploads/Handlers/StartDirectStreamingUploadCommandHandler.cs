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

            await WriteRecordsAsync(records, writer, progress, cancellationToken);
            progress.RecordPeriod(command.StartDate.Value, command.EndDate.Value, records.Count);
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

            await WriteRecordsAsync(records, writer, progress, cancellationToken);
            progress.RecordPeriod(from, to, records.Count);
        }

        async Task WriteRecordsAsync(
            List<DatabaseRecord> records,
            StreamWriter streamWriter,
            UploadProgress uploadProgress,
            CancellationToken ct)
        {
            foreach (var record in records)
            {
                ct.ThrowIfCancellationRequested();
                await streamWriter.WriteLineAsync(lineFormatter.FormatLine(record).AsMemory(), ct);
                uploadProgress.RecordRow();
            }
        }
    }

    static DateOnly Min(DateOnly left, DateOnly right) => left < right ? left : right;
}
