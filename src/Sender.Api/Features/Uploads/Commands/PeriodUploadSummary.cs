namespace Sender.Api;

public sealed record PeriodUploadSummary(DateOnly From, DateOnly To, long Rows);
