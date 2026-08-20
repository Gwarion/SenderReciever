using Microsoft.AspNetCore.Mvc;

namespace Receiver.Api;

[ApiController]
[Route("")]
public sealed class MetricsController : ControllerBase
{
    [HttpGet]
    public IActionResult Index() => Ok(new
    {
        service = "Receiver.Api",
        endpoints = new[]
        {
            "POST /receive",
            "GET /metrics",
            "POST /gc/collect"
        }
    });

    [HttpGet("metrics")]
    public IActionResult GetMetrics() => Ok(ProcessMetrics.Capture());

    [HttpPost("gc/collect")]
    public IActionResult Collect(int generation = 2, bool compact = false)
    {
        generation = Math.Clamp(generation, 0, GC.MaxGeneration);
        GC.Collect(generation, GCCollectionMode.Forced, blocking: true, compacting: compact);
        GC.WaitForPendingFinalizers();

        return Ok(ProcessMetrics.Capture());
    }
}
