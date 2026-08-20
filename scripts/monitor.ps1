param(
    [int[]]$Ports = @(5101, 5102),
    [int]$IntervalSeconds = 2
)

$ErrorActionPreference = 'Stop'

while ($true) {
    Clear-Host
    foreach ($port in $Ports) {
        try {
            $metrics = Invoke-RestMethod "http://localhost:$port/metrics"
            [pscustomobject]@{
                Port = $port
                Pid = $metrics.processId
                WorkingSetMB = [math]::Round($metrics.workingSetBytes / 1000000, 1)
                PrivateMB = [math]::Round($metrics.privateMemoryBytes / 1000000, 1)
                ManagedMB = [math]::Round($metrics.managedAllocatedBytes / 1000000, 1)
                Gen0 = $metrics.gen0Collections
                Gen1 = $metrics.gen1Collections
                Gen2 = $metrics.gen2Collections
                ServerGC = $metrics.isServerGc
            }
        }
        catch {
            [pscustomobject]@{
                Port = $port
                Status = 'not responding'
            }
        }
    }

    Start-Sleep -Seconds $IntervalSeconds
}
