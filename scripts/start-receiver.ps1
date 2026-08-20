param(
    [int]$Port = 5101,
    [string]$OutputDirectory = "$PSScriptRoot\..\artifacts\received"
)

$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot '..\src\Receiver.Api\Receiver.Api.csproj'
$env:ASPNETCORE_URLS = "http://localhost:$Port"
$env:Receiver__OutputDirectory = (Resolve-Path (New-Item -ItemType Directory -Force -Path $OutputDirectory)).Path

dotnet run --project $project --no-restore
