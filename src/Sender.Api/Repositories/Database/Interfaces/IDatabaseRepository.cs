namespace Sender.Api;

public interface IDatabaseRepository
{
    /// <summary>
    /// Fetches records by database-period chunks without materializing full chunks in memory.
    /// </summary>
    /// <param name="command">Validated upload command containing date range and row-count settings.</param>
    /// <param name="cancellationToken">Token used to cancel record fetching.</param>
    /// <returns>An async stream of database chunks.</returns>
    IAsyncEnumerable<DatabaseChunk> FetchChunksAsync(
        StartUploadCommand command,
        CancellationToken cancellationToken);
}
