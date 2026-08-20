using System.Runtime.CompilerServices;

namespace Sender.Api;

sealed class FakeDatabaseRepository : IDatabaseRepository
{
    public async IAsyncEnumerable<DatabaseChunk> FetchChunksAsync(
        StartUploadCommand command,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var random = command.Seed is { } seed ? new Random(seed) : Random.Shared;

        if (command.Rows is { } rows)
        {
            yield return new(command.StartDate!.Value, command.EndDate!.Value, rows, GenerateRecords(rows, cancellationToken));
            yield break;
        }

        for (var from = command.StartDate!.Value; from < command.EndDate!.Value; from = from.AddMonths(command.MonthsPerChunk))
        {
            var to = Min(from.AddMonths(command.MonthsPerChunk), command.EndDate.Value);
            var periodRows = random.NextInt64(command.MinRowsPerChunk, command.MaxRowsPerChunk + 1L);

            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();

            yield return new(from, to, periodRows, GenerateRecords(periodRows, cancellationToken));
        }
    }

    static DateOnly Min(DateOnly left, DateOnly right) => left < right ? left : right;

    static async IAsyncEnumerable<DatabaseRecord> GenerateRecords(
        long rows,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        for (long row = 0; row < rows; row++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return DatabaseRecord.Create();

            if (row % 10_000 == 0)
            {
                await Task.Yield();
            }
        }
    }
}
