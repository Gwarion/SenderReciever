using MediatR;

namespace Sender.Api;

/// <summary>
/// Command handled by MediatR to upload formatted data without periodic manual stream flushing.
/// </summary>
/// <param name="Rows">Optional total row count. When omitted, each period receives a random row count.</param>
/// <param name="StartDate">First date included in the simulated export. Defaults to 2024-01-01.</param>
/// <param name="EndDate">Exclusive export end date. Defaults to today.</param>
/// <param name="MonthsPerChunk">Number of months fetched per database query.</param>
/// <param name="MinRowsPerChunk">Minimum generated records per period when <paramref name="Rows" /> is omitted.</param>
/// <param name="MaxRowsPerChunk">Maximum generated records per period when <paramref name="Rows" /> is omitted.</param>
/// <param name="FlushEveryLines">Accepted for request-shape parity with <see cref="StartUploadCommand" /> but not used for periodic flushing.</param>
/// <param name="ReceiverUrl">Receiver endpoint URL. Defaults to configuration value Receiver:Url.</param>
/// <param name="Seed">Optional random seed for repeatable period row counts.</param>
public sealed record StartDirectStreamingUploadCommand(
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
    /// <summary>
    /// Applies the same defaults and validation as the baseline upload command.
    /// </summary>
    /// <param name="configuration">Application configuration used for receiver URL defaults.</param>
    /// <returns>A validated baseline command used by the repositories.</returns>
    public StartUploadCommand Normalize(IConfiguration configuration) =>
        new StartUploadCommand(
            Rows,
            StartDate,
            EndDate,
            MonthsPerChunk,
            MinRowsPerChunk,
            MaxRowsPerChunk,
            FlushEveryLines,
            ReceiverUrl,
            Seed).Normalize(configuration);
}
