namespace Sender.Api;

public sealed record DatabaseChunk(DateOnly From, DateOnly To, long Rows, IAsyncEnumerable<DatabaseRecord> Records);
