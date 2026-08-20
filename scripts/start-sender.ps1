param(
    [int]$Port = 5102,
    [string]$ReceiverUrl = 'http://localhost:5101/receive'
)

$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot '..\src\Sender.Api\Sender.Api.csproj'
$env:ASPNETCORE_URLS = "http://localhost:$Port"
$env:Receiver__Url = $ReceiverUrl

dotnet run --project $project --no-restore
