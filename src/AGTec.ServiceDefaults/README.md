# AGTec.ServiceDefaults

Cross-cutting host defaults shared by every AGTecFramework service (API, Worker, gRPC): OpenTelemetry,
liveness/readiness health checks, service discovery, and resilient outbound HTTP. One call wires them all;
the instrumentation pipeline is built once and the telemetry sink — OTLP and/or Azure Monitor — is chosen by
configuration, so the pipeline stays provider-portable.

Part of [AGTecFramework](https://www.nuget.org/packages?q=AGTecFramework).

## Install

```bash
dotnet add package AGTec.ServiceDefaults
```

## What it provides

- `AddAGTecServiceDefaults()` — on any `IHostApplicationBuilder`, wires OpenTelemetry, default health checks,
  service discovery, and standard HTTP resilience (timeout, retry, circuit breaker, hedging) for every
  outbound `HttpClient`.
- `ConfigureOpenTelemetry()` / `AddDefaultHealthChecks()` — the building blocks called by
  `AddAGTecServiceDefaults()`, exposed for hosts that want to compose them individually.
- `MapAGTecDefaultEndpoints()` — on a `WebApplication`, maps `/alive` (liveness) and `/health` (readiness).

Tracing captures ASP.NET Core and `HttpClient` (gRPC rides on this) plus the `Wolverine`, `Marten`,
`Npgsql`, and `AGTec.Ai` activity sources — so messaging, persistence, SQL, and AI-chat spans are included,
and Wolverine propagates the trace context across the outbox, linking a publisher's request to the
consuming handler.

## Usage

```csharp
using AGTec.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

// Telemetry, health, service discovery, resilient HTTP.
builder.AddAGTecServiceDefaults();

var app = builder.Build();

// Liveness (/alive) and readiness (/health) probes for Kubernetes.
app.MapAGTecDefaultEndpoints();

app.Run();
```

Works for a non-web `Host` too — call `builder.AddAGTecServiceDefaults()` on a
`Host.CreateApplicationBuilder(args)` (Worker hosts have no endpoints to map).

## Configuration

Exporters are selected by configuration — set one, both, or neither. The same instrumentation (traces,
metrics, logs) feeds whichever sink is configured.

| Key | Default | Purpose |
| --- | --- | --- |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | _(unset)_ | OTLP backend URL (e.g. the Aspire Dashboard in dev). Honours the standard `OTEL_EXPORTER_OTLP_*` variables. |
| `APPLICATIONINSIGHTS_CONNECTION_STRING` | _(unset)_ | Azure Monitor / Application Insights. When set, exports **traces, metrics, and logs** there. |

When neither is set, telemetry stays in-process (no exporter). The Azure Monitor path adds only exporters to
the existing pipeline — it does not re-register instrumentation — so it composes cleanly with the OTLP path.
(Live Metrics, a feature of the `UseAzureMonitor()` AspNetCore distro, is not included.)

## See also

`AGTec.Api` (API edge defaults), `AGTec.Security` (JWT/OIDC), `AGTec.Grpc` (gRPC server/client) — all build
on the telemetry and resilience wired here.
