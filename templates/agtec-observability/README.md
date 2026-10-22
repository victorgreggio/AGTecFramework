# agtec-observability — local traces, logs & metrics with zero cloud

`AGTec.ServiceDefaults` wires OpenTelemetry into every host and exports over **OTLP** whenever
`OTEL_EXPORTER_OTLP_ENDPOINT` is set (the same env var works for any OTLP backend — Azure Monitor's
collector in production, a local dashboard in dev). This template gives you the local dashboard.

## What you get

A standalone **.NET Aspire Dashboard** container that ingests OTLP and shows:

- **Traces** — end-to-end, across services. Because `AGTec.ServiceDefaults` registers the `Wolverine`,
  `Marten`, and `Npgsql` trace sources (and Wolverine propagates the trace context across the outbox), a
  request that publishes a message links all the way to the consumer that handles it — down to the SQL.
- **Structured logs** and **metrics** from ASP.NET Core, HttpClient, and the runtime.

No Azure, no account, no code change — just an env var per service.

## Use it

1. Add the dashboard to your stack (merge the fragment, or copy its one service in):

   ```bash
   docker compose \
     -f docker-compose.yml \
     -f templates/agtec-observability/docker-compose.observability.yml up
   ```

2. Point each service at the collector:

   ```yaml
   api:
     environment:
       OTEL_EXPORTER_OTLP_ENDPOINT: "http://aspire-dashboard:18889"
       OTEL_SERVICE_NAME: "my-api"
   worker:
     environment:
       OTEL_EXPORTER_OTLP_ENDPOINT: "http://aspire-dashboard:18889"
       OTEL_SERVICE_NAME: "my-worker"
   ```

3. Open **http://localhost:18888** and look at **Traces** for an end-to-end view of each flow.

## Deployed

Don't ship this dashboard to production. Instead set `OTEL_EXPORTER_OTLP_ENDPOINT` to your real collector
(or set `APPLICATIONINSIGHTS_CONNECTION_STRING` and add the Azure Monitor exporter) — the instrumentation
in `AGTec.ServiceDefaults` is identical; only the sink changes.

## App-specific spans

To add your own spans, create an `ActivitySource` named after your app and register it in the tracer
(`tracing.AddSource("<your-source>")`). Spans you start on it appear in the same traces, linked by trace id
to the Wolverine/Marten/Npgsql spans AGTec already captures.
