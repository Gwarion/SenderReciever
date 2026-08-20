namespace Sender.Api;

/// <summary>
/// HTTP request used to start the upload proof of concept.
/// </summary>
public sealed record StartUploadRequest(
    long? Rows = null,
    DateOnly? StartDate = null,
    DateOnly? EndDate = null,
    int MonthsPerChunk = 3,
    int MinRowsPerChunk = 100_000,
    int MaxRowsPerChunk = 200_000,
    int FlushEveryLines = 200_000,
    string? ReceiverUrl = null,
    int? Seed = null)
{
    public StartUploadCommand ToCommand() => new(
        Rows,
        StartDate,
        EndDate,
        MonthsPerChunk,
        MinRowsPerChunk,
        MaxRowsPerChunk,
        FlushEveryLines,
        ReceiverUrl,
        Seed);

    public StartDirectStreamingUploadCommand ToDirectStreamingCommand() => new(
        Rows,
        StartDate,
        EndDate,
        MonthsPerChunk,
        MinRowsPerChunk,
        MaxRowsPerChunk,
        FlushEveryLines,
        ReceiverUrl,
        Seed);
}
