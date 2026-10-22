# AGTec.Reference — reference service

A working service that exercises the AGTecFramework stack end to end: CQRS on
**Wolverine** (mediator + pipeline + transactional outbox), **Marten/PostgreSQL**
for the write model and document read model, **RabbitMQ** between services, Minimal
APIs at the edge, and `AGTec.ServiceDefaults` for telemetry and health.

## Layout (Clean / Onion)

```
AGTec.Reference.Domain          # Forecast aggregate, Temperature value object, domain event
      ▲
AGTec.Reference.Application      # Commands/queries, handlers, validators, ports, integration event
      ▲
AGTec.Reference.Infrastructure   # Marten adapters + the shared Wolverine/Marten platform wiring
      ▲
AGTec.Reference.Api              # Minimal API host (write/read via the bus)
AGTec.Reference.Worker           # Read-model projector (consumes the integration event)
AGTec.Reference.Advisory         # Internal gRPC service (synchronous capability)
AGTec.Reference.Grpc.Contracts   # Contract-first .proto (generates client + server)
AGTec.Reference.AppHost          # .NET Aspire local orchestration (Postgres + RabbitMQ + services)
```

## Communication styles (§7)

- **Async events by default** — the write → outbox → projector flow above.
- **gRPC for internal synchronous calls** — `GET /forecasts/{id}/advisory` makes a
  contract-first gRPC call to the Advisory service (address resolved by service
  discovery, resilience inherited from `AGTec.ServiceDefaults`).
- **REST/JSON at the edge only** — the Minimal API endpoints.

## The flow (and why the event can't be lost)

1. `POST /forecasts` → the endpoint sends `RegisterForecast` on the Wolverine bus.
2. `RegisterForecastHandler` creates the `Forecast` aggregate, stages the write via
   Marten, and **returns** `ForecastRegisteredIntegrationEvent`.
3. Wolverine's Marten-backed **transactional outbox** commits the write *and* the
   event in one transaction — no dual write, no lost event.
4. The event is delivered (at-least-once) to the **Worker**, whose
   `ForecastProjectionHandler` upserts a `ForecastSummary` read model (idempotent).

This is proven by `tests/AGTec.Reference.IntegrationTests` against a real PostgreSQL
(Testcontainers): one command → write record **and** projected read model.

## Run locally (Aspire)

```bash
dotnet run --project AGTec.Reference.AppHost
```

Spins up Postgres + RabbitMQ + the API + the Worker with a dashboard, service
discovery, and injected connection strings (`forecasts`, `messaging`).

## Endpoints

| Method | Path | Purpose |
|--------|------|---------|
| GET  | `/alive` | Liveness probe → k8s `livenessProbe` (anonymous) |
| GET  | `/health` | Readiness probe → k8s `readinessProbe` (anonymous) |
| POST | `/forecasts` | Register a forecast → `201 Created` (scope `api`) |
| GET  | `/forecasts/{id}` | Fetch the read DTO → `200` / `404` (scope `api`) |
| GET  | `/forecasts/search?term=` | Full-text search over the search read model (scope `api`) |
| PUT  | `/forecasts/{id}/note` | Store a note **through the app** into the object store (scanned inline) |
| GET  | `/forecasts/{id}/note` | Read the note back from the object store (scope `api`) |
| POST | `/forecasts/{id}/note/upload-url` | Get a SAS URL to upload the note **directly** to storage (AzureBlob only) |
| GET  | `/forecasts/{id}/advisory` | Read DTO + advice from the gRPC Advisory service (scope `api`) |

Invalid input returns an **RFC 9457 Problem Details** `400` (FluentValidation runs
as a Wolverine pipeline step).

## Security & API docs

- **OIDC/JWT bearer** via `AGTec.Security` — `Jwt:Authority`/`Jwt:Audience` from config
  (Entra ID in production; the sample defaults to the Duende demo server). The
  `/forecasts` endpoints require the **`api`** OAuth scope (`401`/`403`/`200`).
- **Native OpenAPI + Scalar** via `AGTec.Api` — in Development, the document is at
  `/openapi/v1.json` and the Scalar UI (with an Authorize button) at `/scalar`.

## Container & Kubernetes

- Chiseled, rootless images: `AGTec.Reference.Api/Dockerfile`,
  `AGTec.Reference.Worker/Dockerfile` (build from the repo root).
- Helm chart: `deploy/helm/agtec-reference` — probes wired to `/alive` + `/health`,
  graceful shutdown, secrets referenced (Key Vault CSI-friendly), and optional
  **KEDA** autoscaling of the Worker on RabbitMQ queue depth.

## Tests

```bash
dotnet test                       # all
dotnet test tests/AGTec.Reference.UnitTests          # domain rules (no Docker)
dotnet test tests/AGTec.Reference.IntegrationTests   # outbox proof (needs Docker)
```

## Config-selected providers (§8 portability)

Both the broker and the search store are chosen by configuration — same binary,
no recompile:

- `AGTec:Messaging:Provider` = `Local` | `RabbitMq` | `AzureServiceBus`
- `AGTec:Search:Provider` = `InMemory` | `Elasticsearch`
- `AGTec:Secrets:Provider` = `Configuration` | `AzureKeyVault` | `Dapr` (`ISecretStore`)
- `AGTec:Storage:Provider` = `InMemory` | `AzureBlob` (`IObjectStore`)
- `AGTec:Storage:Scanner` = `None` | `ClamAv` — malware scanning decorator over `IObjectStore`
  (rejects infected uploads before they are stored)

Proven by tests: the outbox + in-memory search (Postgres), the RabbitMQ transport
round-trip (Postgres + RabbitMQ), the Elasticsearch adapter (real ES), and the Azure
Blob adapter (Azurite). Azure Service Bus and Key Vault are the same config keys.

## Still deferred

Service mesh, per-context event sourcing, and DX niceties (`dotnet new` templates,
CI pipeline) — see `docs/AGTecFramework-Design.md`.
