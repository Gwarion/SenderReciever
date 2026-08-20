namespace Sender.Api;

sealed class UploadProgress(StartUploadCommand command)
{
    readonly List<PeriodUploadSummary> periods = [];

    public long RowsWritten { get; private set; }
    public long BytesWritten { get; private set; }

    public void RecordRow()
    {
        RowsWritten++;
        BytesWritten += UploadLineFormatter.UploadedLineLength;
    }

    public void RecordPeriod(DateOnly from, DateOnly to, long rows) =>
        periods.Add(new(from, to, rows));

    public UploadSummary ToSummary(TimeSpan elapsed) => new(
        command.Rows,
        command.MonthsPerChunk,
        command.StartDate,
        command.EndDate,
        command.MinRowsPerChunk,
        command.MaxRowsPerChunk,
        command.FlushEveryLines,
        RowsWritten,
        BytesWritten,
        periods,
        elapsed);
}
