using System.Net;

namespace Sender.Api;

public sealed record RepositoryUploadResult(HttpStatusCode StatusCode, string ReceiverBody, long CompressedBytesWritten);
