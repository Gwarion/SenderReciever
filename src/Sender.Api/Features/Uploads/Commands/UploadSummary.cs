namespace Sender.Api;

public sealed record UploadSummary(
    long? Rows,
    int MonthsPerChunk,
    DateOnly? StartDate,
    DateOnly? EndDate,
    int MinRowsPerChunk,
    int MaxRowsPerChunk,
    int FlushEveryLines,
    long RowsWritten,
    long BytesWritten,
    IReadOnlyCollection<PeriodUploadSummary> Periods,
    TimeSpan Elapsed);
