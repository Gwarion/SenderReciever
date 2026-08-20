namespace Sender.Api;

public sealed record ProcessMetricsSnapshot(
    int ProcessId,
    string Process,
    DateTimeOffset TimestampUtc,
    long WorkingSetBytes,
    long PrivateMemoryBytes,
    long ManagedAllocatedBytes,
    long GcHeapBytes,
    long GcCommittedBytes,
    long GcFragmentedBytes,
    int Gen0Collections,
    int Gen1Collections,
    int Gen2Collections,
    bool IsServerGc,
    string LatencyMode)
{
    public string ToLogString() =>
        $"pid={ProcessId}, ws={ToMiB(WorkingSetBytes):n1} MiB, private={ToMiB(PrivateMemoryBytes):n1} MiB, managed={ToMiB(ManagedAllocatedBytes):n1} MiB, gen0={Gen0Collections}, gen1={Gen1Collections}, gen2={Gen2Collections}";

    static double ToMiB(long bytes) => bytes / 1024d / 1024d;
}
