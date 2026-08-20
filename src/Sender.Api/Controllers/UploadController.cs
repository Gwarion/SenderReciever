using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Sender.Api;

[ApiController]
[Route("upload")]
public sealed class UploadController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    public Task<StartUploadResponse> StartAsync(
        StartUploadRequest request,
        CancellationToken cancellationToken) =>
        mediator.Send(request.ToCommand(), cancellationToken);
}
