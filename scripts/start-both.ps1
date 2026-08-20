$ErrorActionPreference = 'Stop'

$root = Resolve-Path (Join-Path $PSScriptRoot '..')
$receiver = Join-Path $root 'scripts\start-receiver.ps1'
$sender = Join-Path $root 'scripts\start-sender.ps1'

Start-Process powershell -ArgumentList @('-NoExit', '-ExecutionPolicy', 'Bypass', '-File', $receiver)
Start-Sleep -Seconds 2
Start-Process powershell -ArgumentList @('-NoExit', '-ExecutionPolicy', 'Bypass', '-File', $sender)
