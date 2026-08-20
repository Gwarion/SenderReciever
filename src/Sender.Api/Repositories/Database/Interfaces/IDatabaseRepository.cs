namespace Sender.Api;

public interface IDatabaseRepository
{
    /// <summary>
    /// Fetches records for one database period.
    /// </summary>
    /// <param name="from">Inclusive period start date.</param>
    /// <param name="to">Exclusive period end date.</param>
    /// <param name="minRecords">Minimum records returned for the period.</param>
    /// <param name="maxRecords">Maximum records returned for the period.</param>
    /// <param name="cancellationToken">Token used to cancel record fetching.</param>
    /// <returns>A materialized list of database records.</returns>
    Task<List<DatabaseRecord>> GetAllItemsAsync(
        DateOnly from,
        DateOnly to,
        int minRecords,
        int maxRecords,
        CancellationToken cancellationToken);
}
