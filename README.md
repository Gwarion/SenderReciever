# SenderReceiver

Proof of concept for memory-efficient communication between two local .NET APIs.

## Projects

- `Receiver.Api` runs on `http://localhost:5101` and streams the raw request body directly to one `received.txt` file.
- `Sender.Api` runs on `http://localhost:5102`, simulates DB records in 3-month periods, and streams only the selected upload fields to the receiver through a controller, MediatR command handler, database repository, and upload repository.

Both APIs expose:

- `GET /metrics` for process memory and GC counters.
- `POST /gc/collect?generation=2&compact=false` for explicit GC experiments.

## Run

Restore and build once:

```powershell
dotnet restore SenderReceiver.slnx --configfile NuGet.Config
dotnet build SenderReceiver.slnx --no-restore
```

Start both APIs:

```powershell
.\scripts\start-both.ps1
```

This also opens the upload form at `http://localhost:5102/upload-ui`.

Or start them separately:

```powershell
.\scripts\start-receiver.ps1
.\scripts\start-sender.ps1
```

Monitor both APIs through their own `/metrics` endpoints:

```powershell
.\scripts\monitor.ps1
```

Open the browser upload form:

```text
http://localhost:5102/upload-ui
```

The receiver disables Kestrel's default 30 MB request-body limit for this local POC so large streamed uploads can reach the file writer.
The form also shows sender and receiver RAM evolution over time while polling `/metrics`.

## Send Data

Each fake DB record has six `Guid` fields. The upload line contains only `Field1 + Field2 + newline`, so each uploaded row is 65 bytes.

By default, the sender simulates 3-month DB fetches from `2024-01-01` through today, with `100000` to `200000` records per period:

```powershell
Invoke-RestMethod http://localhost:5102/upload `
  -Method Post `
  -ContentType 'application/json' `
  -Body '{}'
```

Compare the lower-flush strategy:

```powershell
Invoke-RestMethod http://localhost:5102/upload/direct-stream `
  -Method Post `
  -ContentType 'application/json' `
  -Body '{}'
```

Use a smaller request for smoke tests:

```json
{
  "startDate": "2024-01-01",
  "endDate": "2024-07-01",
  "monthsPerChunk": 3,
  "minRowsPerChunk": 1000,
  "maxRowsPerChunk": 2000,
  "flushEveryLines": 200000,
  "receiverUrl": "http://localhost:5101/receive",
  "seed": 123
}
```

When `rows` is omitted, the sender creates one simulated DB chunk per period and randomly chooses the record count for that period between `minRowsPerChunk` and `maxRowsPerChunk`. The sender flushes the outgoing stream every `flushEveryLines`. The receiver writes to disk with a rented buffer and flushes the file every `flushEveryLines`, capped at `200000` lines.

`POST /upload` models the real-world flow. The controller only creates a command and sends it through MediatR. The handler fetches fake database chunks through `IDatabaseRepository`, formats the required fields, and streams through `IUploadRepository`. The upload repository owns the `HttpClient`, wraps the handler's stream writer in a `GZipStream`, and posts the gzip payload.

```powershell
Invoke-RestMethod http://localhost:5102/upload `
  -Method Post `
  -ContentType 'application/json' `
  -Body '{}'
```

- `UploadController` instantiates `StartUploadCommand` and calls `IMediator.Send(...)`.
- `StartUploadCommandHandler` fetches records and writes upload lines into the stream it is given.
- `IDatabaseRepository` exposes the fake 3-month database fetch.
- `IUploadRepository` exposes `UploadGzipAsync(...)` and owns HTTP transport details.
- `HttpUploadRepository` is registered through `AddHttpClient<IUploadRepository, HttpUploadRepository>()`.
- `GzipStreamingUploadHttpContent` compresses while `HttpClient` pulls bytes, so neither handler nor repository has to buffer the upload in memory.
- `StartDirectStreamingUploadCommandHandler` is the recommended comparison path: it writes directly to the repository stream and does not periodically call `FlushAsync`.

For deeper GC observation, use the process IDs from `/metrics` with:

```powershell
dotnet-counters monitor --process-id <pid> System.Runtime
```
