namespace Sender.Api;

public sealed record StartUploadResponse(
    UploadSummary Upload,
    RepositoryUploadResult Repository,
    ProcessMetricsSnapshot Sender);
