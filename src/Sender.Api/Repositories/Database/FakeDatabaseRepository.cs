namespace Sender.Api;

sealed class FakeDatabaseRepository : IDatabaseRepository
{
    static readonly Random Random = new();

    public Task<List<DatabaseRecord>> GetAllItemsAsync(
        DateOnly from,
        DateOnly to,
        int minRecords,
        int maxRecords,
        CancellationToken cancellationToken)
    {
        var recordCount = NextRecordCount(minRecords, maxRecords);
        var records = new List<DatabaseRecord>();

        for (var index = 0; index < recordCount; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            records.Add(DatabaseRecord.Create());
        }

        return Task.FromResult(records);
    }

    static int NextRecordCount(int minRecords, int maxRecords)
        => (int)Random.NextInt64(minRecords, (long)maxRecords + 1);
}
