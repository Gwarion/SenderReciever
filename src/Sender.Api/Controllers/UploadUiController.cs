using Microsoft.AspNetCore.Mvc;

namespace Sender.Api;

[Route("upload-ui")]
public sealed class UploadUiController : Controller
{
    [HttpGet]
    public ContentResult Index()
    {
        Response.Headers.CacheControl = "no-store";
        return Content(BuildHtml(), "text/html");
    }

    static string BuildHtml()
    {
        var endDate = DateOnly.FromDateTime(DateTime.Today);
        var defaultJson = $$"""
            {
              "startDate": "2024-01-01",
              "endDate": "{{endDate:yyyy-MM-dd}}",
              "monthsPerChunk": 3,
              "minRowsPerChunk": 100000,
              "maxRowsPerChunk": 200000,
              "receiverUrl": "http://localhost:5101/receive"
            }
            """;

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
            textarea, select, button {
              font: inherit;
              padding: 9px 10px;
              border: 1px solid color-mix(in srgb, CanvasText 24%, transparent);
              border-radius: 6px;
              background: Canvas;
              color: CanvasText;
            }
            textarea {
              min-height: 220px;
              resize: vertical;
              font-family: Consolas, ui-monospace, monospace;
              font-size: 13px;
              line-height: 1.45;
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
            .form-row {
              display: grid;
              grid-template-columns: minmax(260px, 1fr) auto auto;
              gap: 10px;
              align-items: end;
            }
            .metrics {
              display: grid;
              grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
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
            .status {
              margin: 14px 0;
              font-weight: 650;
            }
            .status.sending {
              color: #2563eb;
            }
            .status.success {
              color: #0f766e;
            }
            .status.error {
              color: #dc2626;
            }
            .elapsed {
              display: inline-block;
              margin-left: 8px;
              padding: 2px 8px;
              border-radius: 999px;
              background: #0f766e;
              color: white;
              font-variant-numeric: tabular-nums;
            }
            .charts {
              display: grid;
              grid-template-columns: repeat(auto-fit, minmax(420px, 1fr));
              gap: 14px;
              margin: 18px 0;
            }
            .chart {
              width: 100%;
              height: 220px;
              display: block;
              background: color-mix(in srgb, CanvasText 5%, Canvas);
              border-radius: 6px;
            }
            .legend {
              display: flex;
              flex-wrap: wrap;
              gap: 12px;
              margin-top: 10px;
              font-size: 13px;
            }
            .legend span {
              display: inline-flex;
              align-items: center;
              gap: 6px;
            }
            .swatch {
              width: 16px;
              height: 3px;
              border-radius: 999px;
            }
            @media (max-width: 700px) {
              .form-row {
                grid-template-columns: 1fr;
              }
            }
          </style>
        </head>
        <body>
          <main>
            <h1>Sender Upload POC</h1>
            <form id="upload-form">
              <div class="form-row">
                <label>
                  Endpoint
                  <select id="endpoint">
                    <option value="/upload/direct-stream">Direct stream, no periodic flush</option>
                    <option value="/upload">Baseline, flush checkpoints</option>
                  </select>
                </label>
                <button class="primary" type="submit">Send Upload</button>
                <button type="button" id="metrics-refresh">Refresh Metrics</button>
              </div>
              <label>
                Request JSON
                <textarea id="request-json" spellcheck="false">{{defaultJson}}</textarea>
              </label>
            </form>

            <div class="status" id="status">Ready.</div>

            <section class="metrics">
              <div class="panel">
                <h2>Sender Summary</h2>
                <dl id="sender-summary"></dl>
              </div>
              <div class="panel">
                <h2>Receiver Summary</h2>
                <dl id="receiver-summary"></dl>
              </div>
            </section>

            <section class="charts">
              <div class="panel">
                <h2>Sender RAM Over Time</h2>
                <svg class="chart" id="sender-chart" viewBox="0 0 640 220" role="img" aria-label="Sender RAM over time"></svg>
                <div class="legend">
                  <span><i class="swatch" style="background:#0f766e"></i>Working set</span>
                  <span><i class="swatch" style="background:#2563eb"></i>Private</span>
                  <span><i class="swatch" style="background:#dc2626"></i>Managed</span>
                </div>
              </div>
              <div class="panel">
                <h2>Receiver RAM Over Time</h2>
                <svg class="chart" id="receiver-chart" viewBox="0 0 640 220" role="img" aria-label="Receiver RAM over time"></svg>
                <div class="legend">
                  <span><i class="swatch" style="background:#0f766e"></i>Working set</span>
                  <span><i class="swatch" style="background:#2563eb"></i>Private</span>
                  <span><i class="swatch" style="background:#dc2626"></i>Managed</span>
                </div>
              </div>
            </section>
          </main>

          <script>
            const form = document.getElementById('upload-form');
            const statusEl = document.getElementById('status');
            const requestJson = document.getElementById('request-json');
            const senderSummary = document.getElementById('sender-summary');
            const receiverSummary = document.getElementById('receiver-summary');
            const senderChart = document.getElementById('sender-chart');
            const receiverChart = document.getElementById('receiver-chart');
            const endpointSelect = document.getElementById('endpoint');
            const histories = { sender: [], receiver: [] };
            const run = { initial: null, start: null, end: null, max: null };
            const maxSamples = 240;
            const metricsPollingMilliseconds = 500;
            let metricsTimer;

            function mbValue(bytes) {
              return bytes == null ? null : bytes / 1000 / 1000;
            }

            function formatMb(value) {
              return value == null ? '-' : `${value.toFixed(1)} MB`;
            }

            function formatBytes(bytes) {
              if (bytes == null) {
                return '-';
              }

              const gb = bytes / 1000 / 1000 / 1000;
              if (gb >= 1) {
                return `${gb.toFixed(2)} GB`;
              }

              return `${(bytes / 1000 / 1000).toFixed(1)} MB`;
            }

            function formatDuration(milliseconds) {
              if (milliseconds < 1000) {
                return `${milliseconds.toFixed(0)} ms`;
              }

              const seconds = milliseconds / 1000;
              if (seconds < 60) {
                return `${seconds.toFixed(2)} s`;
              }

              const minutes = Math.floor(seconds / 60);
              return `${minutes} min ${(seconds % 60).toFixed(1)} s`;
            }

            function setStatus(message, state, elapsedMilliseconds) {
              statusEl.className = `status ${state ?? ''}`.trim();
              statusEl.innerHTML = elapsedMilliseconds == null
                ? message
                : `${message}<span class="elapsed">${formatDuration(elapsedMilliseconds)}</span>`;
            }

            function toSnapshot(metrics) {
              return {
                processId: metrics.processId,
                workingSet: mbValue(metrics.workingSetBytes),
                privateMemory: mbValue(metrics.privateMemoryBytes),
                managed: mbValue(metrics.managedAllocatedBytes),
                gen0: metrics.gen0Collections,
                gen1: metrics.gen1Collections,
                gen2: metrics.gen2Collections
              };
            }

            function remember(history, metrics) {
              const snapshot = {
                timestamp: Date.now(),
                ...toSnapshot(metrics)
              };

              history.push(snapshot);
              if (history.length > maxSamples) {
                history.shift();
              }

              return snapshot;
            }

            function updateMax(current) {
              if (!run.max) {
                run.max = structuredClone(current);
                return;
              }

              for (const key of ['sender', 'receiver']) {
                if (current[key].workingSet > run.max[key].workingSet) {
                  run.max[key] = current[key];
                }
              }
            }

            function totalGc(snapshot) {
              return snapshot == null ? null : snapshot.gen0 + snapshot.gen1 + snapshot.gen2;
            }

            function gcDelta(from, to) {
              if (!from || !to) {
                return '-';
              }

              return `${totalGc(to) - totalGc(from)} total (${to.gen0 - from.gen0}/${to.gen1 - from.gen1}/${to.gen2 - from.gen2})`;
            }

            function renderSummary(target, key) {
              const initial = run.initial?.[key];
              const start = run.start?.[key];
              const end = run.end?.[key];
              const max = run.max?.[key];
              target.innerHTML = '';

              const rows = [
                ['PID', end?.processId ?? start?.processId ?? initial?.processId ?? '-'],
                ['Starting RAM', formatMb(initial?.workingSet)],
                ['RAM at send start', formatMb(start?.workingSet)],
                ['RAM at receive end', formatMb(end?.workingSet)],
                ['Max RAM reached', formatMb(max?.workingSet)],
                ['GC during run', gcDelta(start, end)]
              ];

              for (const [name, value] of rows) {
                const dt = document.createElement('dt');
                const dd = document.createElement('dd');
                dt.textContent = name;
                dd.textContent = value;
                target.append(dt, dd);
              }
            }

            function renderSummaries() {
              renderSummary(senderSummary, 'sender');
              renderSummary(receiverSummary, 'receiver');
            }

            function pathFor(points, key, minValue, maxValue) {
              if (points.length === 0) {
                return '';
              }

              const width = 600;
              const height = 160;
              const left = 28;
              const top = 22;
              const scale = Math.max(maxValue - minValue, 1);

              return points.map((point, index) => {
                const x = left + (points.length === 1 ? width : index * width / (points.length - 1));
                const y = top + height - ((point[key] - minValue) / scale * height);
                return `${x.toFixed(1)},${y.toFixed(1)}`;
              }).join(' ');
            }

            function renderChart(svg, history) {
              const values = history.flatMap(sample => [
                sample.workingSet,
                sample.privateMemory,
                sample.managed
              ]);

              const maxValue = Math.max(64, ...values);
              const minValue = 0;
              const latest = history.at(-1);
              const label = latest
                ? `Latest WS ${latest.workingSet.toFixed(1)} MB, Private ${latest.privateMemory.toFixed(1)} MB, Managed ${latest.managed.toFixed(1)} MB`
                : 'Waiting for metrics';

              svg.innerHTML = `
                <text x="28" y="18" fill="currentColor" font-size="12">${maxValue.toFixed(0)} MB</text>
                <line x1="28" y1="182" x2="628" y2="182" stroke="currentColor" opacity=".22"></line>
                <line x1="28" y1="22" x2="28" y2="182" stroke="currentColor" opacity=".22"></line>
                <polyline points="${pathFor(history, 'workingSet', minValue, maxValue)}" fill="none" stroke="#0f766e" stroke-width="2.5"></polyline>
                <polyline points="${pathFor(history, 'privateMemory', minValue, maxValue)}" fill="none" stroke="#2563eb" stroke-width="2.5"></polyline>
                <polyline points="${pathFor(history, 'managed', minValue, maxValue)}" fill="none" stroke="#dc2626" stroke-width="2.5"></polyline>
                <text x="28" y="208" fill="currentColor" font-size="12">${label}</text>
              `;
            }

            async function fetchJson(url) {
              const response = await fetch(url);
              if (!response.ok) {
                throw new Error(`${url} returned ${response.status}`);
              }
              return response.json();
            }

            async function refreshMetrics() {
              const [sender, receiver] = await Promise.all([
                fetchJson('/metrics'),
                fetchJson('http://localhost:5101/metrics')
              ]);
              const current = {
                sender: remember(histories.sender, sender),
                receiver: remember(histories.receiver, receiver)
              };

              run.initial ??= structuredClone(current);
              updateMax(current);
              renderChart(senderChart, histories.sender);
              renderChart(receiverChart, histories.receiver);
              renderSummaries();

              return current;
            }

            form.addEventListener('submit', async event => {
              event.preventDefault();
              const requestStarted = performance.now();

              let body;
              try {
                body = JSON.parse(requestJson.value);
              } catch (error) {
                setStatus(`Invalid JSON: ${error.message}`, 'error');
                return;
              }

              const endpoint = endpointSelect.value;
              setStatus(`Sending to ${endpoint}...`, 'sending');
              clearInterval(metricsTimer);

              try {
                run.start = await refreshMetrics();
                run.end = null;
                run.max = structuredClone(run.start);
                histories.sender.length = 0;
                histories.receiver.length = 0;
                histories.sender.push(run.start.sender);
                histories.receiver.push(run.start.receiver);
                renderChart(senderChart, histories.sender);
                renderChart(receiverChart, histories.receiver);
                metricsTimer = setInterval(async () => {
                  try {
                    await refreshMetrics();
                  } catch (error) {
                    setStatus(error.message, 'error');
                  }
                }, metricsPollingMilliseconds);

                const response = await fetch(endpoint, {
                  method: 'POST',
                  headers: { 'Content-Type': 'application/json' },
                  body: JSON.stringify(body)
                });
                const text = await response.text();
                const result = text ? JSON.parse(text) : {};

                if (!response.ok) {
                  throw new Error(result.title ?? result.detail ?? `Request failed with ${response.status}`);
                }

                run.end = await refreshMetrics();
                setStatus(
                  `Completed. Rows ${result.upload?.rowsWritten ?? '-'}, size ${formatBytes(result.upload?.bytesWritten)}.`,
                  'success',
                  performance.now() - requestStarted);
              } catch (error) {
                run.end = await refreshMetrics().catch(() => run.end);
                setStatus(error.message, 'error', performance.now() - requestStarted);
              } finally {
                clearInterval(metricsTimer);
                renderSummaries();
              }
            });

            document.getElementById('metrics-refresh').addEventListener('click', async () => {
              try {
                await refreshMetrics();
              } catch (error) {
                setStatus(error.message, 'error');
              }
            });

            refreshMetrics().catch(error => {
              setStatus(error.message, 'error');
            });
          </script>
        </body>
        </html>
        """;
    }
}
