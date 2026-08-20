using System.Diagnostics;
using System.Runtime;

namespace Receiver.Api;

static class ProcessMetrics
{
    public static ProcessMetricsSnapshot Capture()
    {
        using var process = Process.GetCurrentProcess();
        var gc = GC.GetGCMemoryInfo();

        return new(
            Environment.ProcessId,
            Environment.ProcessPath ?? process.ProcessName,
            DateTimeOffset.UtcNow,
            process.WorkingSet64,
            process.PrivateMemorySize64,
            GC.GetTotalMemory(forceFullCollection: false),
            gc.HeapSizeBytes,
            gc.TotalCommittedBytes,
            gc.FragmentedBytes,
            GC.CollectionCount(0),
            GC.CollectionCount(1),
            GC.CollectionCount(2),
            GCSettings.IsServerGC,
            GCSettings.LatencyMode.ToString());
    }
}
