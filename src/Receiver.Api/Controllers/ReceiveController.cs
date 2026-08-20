using Microsoft.AspNetCore.Mvc;

namespace Receiver.Api;

[ApiController]
[Route("receive")]
public sealed class ReceiveController(ReceiveRequestHandler handler) : ControllerBase
{
    [HttpPost]
    public Task<IResult> ReceiveAsync(CancellationToken cancellationToken) =>
        handler.ReceiveAsync(HttpContext, cancellationToken);
}
