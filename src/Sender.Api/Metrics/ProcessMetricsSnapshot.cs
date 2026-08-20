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
        $"pid={ProcessId}, ws={ToMb(WorkingSetBytes):n1} MB, private={ToMb(PrivateMemoryBytes):n1} MB, managed={ToMb(ManagedAllocatedBytes):n1} MB, gen0={Gen0Collections}, gen1={Gen1Collections}, gen2={Gen2Collections}";

    static double ToMb(long bytes) => bytes / 1000d / 1000d;
}
