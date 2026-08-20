using MediatR;

namespace Sender.Api;

/// <summary>
/// Command handled by MediatR to fetch database records and upload formatted data.
/// </summary>
/// <param name="Rows">Optional total row count. When omitted, each period receives a random row count.</param>
/// <param name="StartDate">First date included in the simulated export. Defaults to 2024-01-01.</param>
/// <param name="EndDate">Exclusive export end date. Defaults to today.</param>
/// <param name="MonthsPerChunk">Number of months fetched per database query.</param>
/// <param name="MinRowsPerChunk">Minimum generated records per period when <paramref name="Rows" /> is omitted.</param>
/// <param name="MaxRowsPerChunk">Maximum generated records per period when <paramref name="Rows" /> is omitted.</param>
/// <param name="FlushEveryLines">Maximum number of logical rows written before flushing the upload stream.</param>
/// <param name="ReceiverUrl">Receiver endpoint URL. Defaults to configuration value Receiver:Url.</param>
/// <param name="Seed">Optional random seed for repeatable period row counts.</param>
public sealed record StartUploadCommand(
    long? Rows = null,
    DateOnly? StartDate = null,
    DateOnly? EndDate = null,
    int MonthsPerChunk = 3,
    int MinRowsPerChunk = 100_000,
    int MaxRowsPerChunk = 200_000,
    int FlushEveryLines = 200_000,
    string? ReceiverUrl = null,
    int? Seed = null) : IRequest<StartUploadResponse>
{
    public Uri ReceiverUri => new(ReceiverUrl!, UriKind.Absolute);

    /// <summary>
    /// Applies defaults from configuration and validates the command.
    /// </summary>
    /// <param name="configuration">Application configuration used for receiver URL defaults.</param>
    /// <returns>A validated command.</returns>
    public StartUploadCommand Normalize(IConfiguration configuration)
    {
        var startDate = StartDate ?? new DateOnly(2024, 1, 1);
        var endDate = EndDate ?? DateOnly.FromDateTime(DateTimeOffset.Now.DateTime);
        var receiverUrl = ReceiverUrl ?? configuration["Receiver:Url"] ?? "http://localhost:5101/receive";

        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(endDate.DayNumber, startDate.DayNumber, nameof(EndDate));

        if (Rows is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(Rows), "Rows must be greater than zero when specified.");
        }

        return this with
        {
            StartDate = startDate,
            EndDate = endDate,
            MonthsPerChunk = MonthsPerChunk is > 0 and <= 12
                ? MonthsPerChunk
                : throw new ArgumentOutOfRangeException(nameof(MonthsPerChunk), "MonthsPerChunk must be between 1 and 12."),
            MinRowsPerChunk = MinRowsPerChunk > 0
                ? MinRowsPerChunk
                : throw new ArgumentOutOfRangeException(nameof(MinRowsPerChunk), "MinRowsPerChunk must be greater than zero."),
            MaxRowsPerChunk = MaxRowsPerChunk >= MinRowsPerChunk
                ? MaxRowsPerChunk
                : throw new ArgumentOutOfRangeException(nameof(MaxRowsPerChunk), "MaxRowsPerChunk must be greater than or equal to MinRowsPerChunk."),
            FlushEveryLines = FlushEveryLines is > 0 and <= 200_000
                ? FlushEveryLines
                : throw new ArgumentOutOfRangeException(nameof(FlushEveryLines), "FlushEveryLines must be between 1 and 200,000."),
            ReceiverUrl = receiverUrl
        };
    }
}
