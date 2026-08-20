using Microsoft.AspNetCore.Mvc;

namespace Sender.Api;

[Route("upload-ui")]
public sealed class UploadUiController : Controller
{
    [HttpGet]
    public ContentResult Index() => Content(BuildHtml(), "text/html");

    static string BuildHtml()
    {
        var endDate = DateOnly.FromDateTime(DateTimeOffset.Now.DateTime);

        return $$"""
        <!doctype html>
        <html lang="en">
        <head>
          <meta charset="utf-8">
          <meta name="viewport" content="width=device-width, initial-scale=1">
          <title>Sender Upload POC</title>
          <style>
            :root {
              color-scheme: light dark;
              font-family: Segoe UI, system-ui, sans-serif;
              line-height: 1.4;
            }
            body {
              margin: 0;
              background: Canvas;
              color: CanvasText;
            }
            main {
              max-width: 1100px;
              margin: 0 auto;
              padding: 28px;
            }
            h1 {
              margin: 0 0 20px;
              font-size: 28px;
              font-weight: 650;
            }
            form {
              display: grid;
              grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
              gap: 14px;
              padding: 18px;
              border: 1px solid color-mix(in srgb, CanvasText 20%, transparent);
              border-radius: 8px;
            }
            label {
              display: grid;
              gap: 6px;
              font-size: 13px;
              font-weight: 600;
            }
            input, select, button {
              font: inherit;
              padding: 9px 10px;
              border: 1px solid color-mix(in srgb, CanvasText 24%, transparent);
              border-radius: 6px;
              background: Canvas;
              color: CanvasText;
            }
            button {
              cursor: pointer;
              font-weight: 700;
            }
            button.primary {
              background: #0f766e;
              border-color: #0f766e;
              color: white;
            }
            .actions {
              display: flex;
              align-items: end;
              gap: 10px;
            }
            .metrics {
              display: grid;
              grid-template-columns: repeat(auto-fit, minmax(260px, 1fr));
              gap: 14px;
              margin: 18px 0;
            }
            .panel {
              border: 1px solid color-mix(in srgb, CanvasText 20%, transparent);
              border-radius: 8px;
              padding: 14px;
            }
            .panel h2 {
              margin: 0 0 10px;
              font-size: 16px;
            }
            dl {
              display: grid;
              grid-template-columns: 1fr auto;
              gap: 6px 12px;
              margin: 0;
              font-size: 13px;
            }
            dd {
              margin: 0;
              font-variant-numeric: tabular-nums;
            }
            pre {
              min-height: 220px;
              overflow: auto;
              padding: 14px;
              border-radius: 8px;
              background: color-mix(in srgb, CanvasText 8%, Canvas);
              white-space: pre-wrap;
            }
            .status {
              margin: 14px 0;
              font-weight: 650;
            }
          </style>
        </head>
        <body>
          <main>
            <h1>Sender Upload POC</h1>
            <form id="upload-form">
              <label>
                Endpoint
                <select id="endpoint">
                  <option value="/upload/direct-stream">Direct stream, no periodic flush</option>
                  <option value="/upload">Baseline, flush checkpoints</option>
                </select>
              </label>
              <label>
                Start date
                <input id="startDate" type="date" value="2024-01-01">
              </label>
              <label>
                End date
                <input id="endDate" type="date" value="{{endDate:yyyy-MM-dd}}">
              </label>
              <label>
                Months per chunk
                <input id="monthsPerChunk" type="number" value="3" min="1" max="12">
              </label>
              <label>
                Min rows per chunk
                <input id="minRowsPerChunk" type="number" value="100000" min="1">
              </label>
              <label>
                Max rows per chunk
                <input id="maxRowsPerChunk" type="number" value="200000" min="1">
              </label>
              <label id="flush-field">
                Flush every lines
                <input id="flushEveryLines" type="number" value="200000" min="1" max="200000">
              </label>
              <label>
                Receiver URL
                <input id="receiverUrl" type="url" value="http://localhost:5101/receive">
              </label>
              <label>
                Seed
                <input id="seed" type="number" placeholder="optional">
              </label>
              <label>
                Fixed rows
                <input id="rows" type="number" min="1" placeholder="leave empty for chunk ranges">
              </label>
              <div class="actions">
                <button class="primary" type="submit">Send Upload</button>
                <button type="button" id="metrics-refresh">Refresh Metrics</button>
              </div>
            </form>

            <div class="status" id="status">Ready.</div>

            <section class="metrics">
              <div class="panel">
                <h2>Sender</h2>
                <dl id="sender-metrics"></dl>
              </div>
              <div class="panel">
                <h2>Receiver</h2>
                <dl id="receiver-metrics"></dl>
              </div>
            </section>

            <pre id="output">{}</pre>
          </main>

          <script>
            const form = document.getElementById('upload-form');
            const statusEl = document.getElementById('status');
            const output = document.getElementById('output');
            const senderMetrics = document.getElementById('sender-metrics');
            const receiverMetrics = document.getElementById('receiver-metrics');
            const endpointSelect = document.getElementById('endpoint');
            const flushField = document.getElementById('flush-field');
            const flushInput = document.getElementById('flushEveryLines');
            let metricsTimer;

            function readNumber(id) {
              const value = document.getElementById(id).value;
              return value === '' ? null : Number(value);
            }

            function readText(id) {
              const value = document.getElementById(id).value.trim();
              return value === '' ? null : value;
            }

            function buildRequest() {
              const body = {
                rows: readNumber('rows'),
                startDate: readText('startDate'),
                endDate: readText('endDate'),
                monthsPerChunk: readNumber('monthsPerChunk'),
                minRowsPerChunk: readNumber('minRowsPerChunk'),
                maxRowsPerChunk: readNumber('maxRowsPerChunk'),
                receiverUrl: readText('receiverUrl'),
                seed: readNumber('seed')
              };

              if (endpointSelect.value === '/upload') {
                body.flushEveryLines = readNumber('flushEveryLines');
              }

              for (const key of Object.keys(body)) {
                if (body[key] === null) {
                  delete body[key];
                }
              }

              return body;
            }

            function syncFlushField() {
              const usesFlush = endpointSelect.value === '/upload';
              flushField.style.display = usesFlush ? 'grid' : 'none';
              flushInput.disabled = !usesFlush;
            }

            function mib(bytes) {
              return bytes == null ? '' : `${(bytes / 1024 / 1024).toFixed(1)} MiB`;
            }

            function renderMetrics(target, metrics) {
              target.innerHTML = '';
              const rows = [
                ['PID', metrics.processId],
                ['Working set', mib(metrics.workingSetBytes)],
                ['Private memory', mib(metrics.privateMemoryBytes)],
                ['Managed memory', mib(metrics.managedAllocatedBytes)],
                ['GC heap', mib(metrics.gcHeapBytes)],
                ['Gen 0', metrics.gen0Collections],
                ['Gen 1', metrics.gen1Collections],
                ['Gen 2', metrics.gen2Collections],
                ['Server GC', metrics.isServerGc]
              ];

              for (const [name, value] of rows) {
                const dt = document.createElement('dt');
                const dd = document.createElement('dd');
                dt.textContent = name;
                dd.textContent = value;
                target.append(dt, dd);
              }
            }

            async function fetchJson(url) {
              const response = await fetch(url);
              if (!response.ok) {
                throw new Error(`${url} returned ${response.status}`);
              }
              return response.json();
            }

            async function refreshMetrics() {
              try {
                const [sender, receiver] = await Promise.all([
                  fetchJson('/metrics'),
                  fetchJson('http://localhost:5101/metrics')
                ]);
                renderMetrics(senderMetrics, sender);
                renderMetrics(receiverMetrics, receiver);
              } catch (error) {
                statusEl.textContent = error.message;
              }
            }

            form.addEventListener('submit', async event => {
              event.preventDefault();
              const endpoint = endpointSelect.value;
              const body = buildRequest();

              statusEl.textContent = `Sending to ${endpoint}...`;
              output.textContent = JSON.stringify(body, null, 2);
              clearInterval(metricsTimer);
              metricsTimer = setInterval(refreshMetrics, 1500);

              try {
                const response = await fetch(endpoint, {
                  method: 'POST',
                  headers: { 'Content-Type': 'application/json' },
                  body: JSON.stringify(body)
                });
                const text = await response.text();
                const result = text ? JSON.parse(text) : {};

                output.textContent = JSON.stringify(result, null, 2);
                statusEl.textContent = response.ok
                  ? `Completed with ${response.status}.`
                  : `Failed with ${response.status}.`;
              } catch (error) {
                output.textContent = error.stack ?? String(error);
                statusEl.textContent = 'Request failed.';
              } finally {
                clearInterval(metricsTimer);
                await refreshMetrics();
              }
            });

            document.getElementById('metrics-refresh').addEventListener('click', refreshMetrics);
            endpointSelect.addEventListener('change', syncFlushField);
            syncFlushField();
            refreshMetrics();
          </script>
        </body>
        </html>
        """;
    }
}
