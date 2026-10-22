# AGTecFramework

An opinionated, modern foundation for building enterprise microservices on
**.NET 10 / C# 14**. AGTecFramework keeps the good ideas — CQRS,
Domain-Driven Design, Clean Architecture, event-driven microservices — and
replaces hand-rolled plumbing with mature, well-licensed ecosystem libraries.

> **Status:** Design phase. No framework code yet — we are agreeing the plan first.

## Start here

📄 **[AGTecFramework — Design & Architecture Plan](docs/AGTecFramework-Design.md)**

That document covers the vision, an honest assessment of the existing libraries,
the "own the architecture, buy the plumbing" build-vs-buy analysis (including the
2025 licensing shifts in MediatR / AutoMapper / MassTransit), component-by-component
decisions, the gRPC-vs-REST-vs-async communication model, the Azure-first-but-portable
cloud strategy, AKS/container deployment, reliability patterns, the proposed package
layout, and a phased roadmap.

## Decisions locked

- **Stack:** [Wolverine](https://wolverinefx.net/) (MIT) for the in-process CQRS
  mediator **and** messaging/transactional outbox — one library, fixes the
  lost-event gap.
- **Broker:** Azure Service Bus by default, RabbitMQ locally, Kafka optional —
  selected by configuration.
- **Data:** [Marten](https://martendb.io/) on PostgreSQL for write + document +
  (optional) event store; Elasticsearch/OpenSearch for search.
- **Edge:** Minimal APIs (REST at the edge); gRPC for internal sync calls; async
  events by default.
- **Cloud:** Azure-first, not Azure-only — provider-coupled resources sit behind
  abstractions swapped by **config at runtime (+ restart), not recompilation**.
- **Deployment:** OCI containers on **Kubernetes (AKS)**, portable to any CNCF cluster.
- **Testing:** xUnit + AwesomeAssertions + AutoFixture/Bogus + Testcontainers.

See the full [decision log (§14)](docs/AGTecFramework-Design.md#14-open-decisions) —
all foundational decisions are resolved.

## Guiding principles

- **Own the architecture, buy the plumbing.** Our value is conventions + domain,
  not a bespoke message bus.
- **Async & event-driven by default; synchronous (gRPC) by exception; REST at the
  edge only.**
- **Reliability designed-in:** transactional outbox, idempotent consumers, retries,
  dead-lettering, health, and tracing ship in the template.
- **Rich domain models, each service owning its own data.**
- **Azure-first, not Azure-only; AKS-ready by default.**
- **Observable and testable by construction.**

## Implementation status

Decisions are resolved (see the [decision log (§14)](docs/AGTecFramework-Design.md#14-open-decisions)).
Building up the framework and a working reference service:

- ✅ **Foundations:** `AGTec.ServiceDefaults` (OTel/health/resilience), `AGTec.Domain`
  (aggregate/entity/domain-event primitives), `AGTec.Application` (CQRS + Wolverine
  pipeline), `AGTec.Messaging`, Central Package Management + analyzers.
- ✅ **Reference service** (`samples/AGTec.Reference`): layered CQRS on **Wolverine +
  Marten/PostgreSQL** with the **transactional outbox**, a read-model **Worker**
  projector feeding a Marten document model and an **Elasticsearch** search index,
  Minimal APIs, and an **Aspire** AppHost. The outbox is proven end to end by a
  Testcontainers integration test.
- ✅ **Config-selected providers** (§8 portability): `AGTec:Messaging:Provider`
  (Local | RabbitMq | AzureServiceBus) and `AGTec:Search:Provider` (InMemory |
  Elasticsearch) — broker and search store swapped by config, not code. Proven by
  Testcontainers tests (RabbitMQ round-trip, real Elasticsearch adapter).
- ✅ **Communication styles** (§7): async events by default, **gRPC** for internal
  sync calls (`AGTec.Grpc`, contract-first `.proto`), REST at the edge.
- ✅ **Edge hardening:** `AGTec.Security` (OIDC/JWT + OAuth scope authorization) and
  `AGTec.Api` (native OpenAPI with the bearer scheme + Scalar UI).
- ✅ **Cloud provider ports** (§8): `AGTec.Platform` (provider-neutral `ISecretStore` /
  `IObjectStore` with in-memory and **file-system** defaults) and `AGTec.Platform.Azure`
  (Key Vault + Blob adapters) — selected by config (`AGTec:Secrets:Provider`,
  `AGTec:Storage:Provider` = `InMemory` | `FileSystem` | `AzureBlob`). The Blob adapter
  is proven against Azurite; the file-system adapter guards against path traversal.
- ✅ **Malware scanning** (`IMalwareScanner`): a decorator over `IObjectStore` rejects
  infected uploads before they are stored. Adapters: no-op (default — defer to the
  storage layer) and **ClamAV** (`AGTec.Platform.ClamAv`), proven against a real clamd
  with the EICAR signature. Selected by `AGTec:Storage:Scanner`.
- ✅ **Direct uploads** (`IObjectUrlProvider`): Azure Blob **SAS** adapter issues
  short-lived signed URLs so clients up/download directly to storage, bypassing the app
  (pair with Defender for Storage since inline scanning is skipped).
- ✅ **AI / RAG building blocks:** `AGTec.Ai` — a no-model **deterministic embedding
  generator** on the standard `Microsoft.Extensions.AI.IEmbeddingGenerator` and a
  no-model **`StubChatClient`** on `IChatClient`, so agent/chat code runs end to end with
  no model or cloud; `AGTec.Ai.OpenAI` plugs **OpenAI** and **Azure OpenAI** chat models
  in behind the same `IChatClient` (Azure uses managed identity / `DefaultAzureCredential`
  by default), selected by config. `AGTec.VectorData` — a partition-scoped `IVectorStore`
  (in-memory default) with a **pgvector** adapter (`AGTec.VectorData.PgVector`, proven
  against real Postgres) and Azure AI Search as the remote option.
- ✅ **Content extraction** (`AGTec.Content`): an `ITextExtractor` port + paragraph-aware
  `ITextChunker` for RAG ingestion, with a local adapter (`AGTec.Content.Local`) for plain
  text, **PDF** (PdfPig) and **Word** (.docx via OpenXml) — no cloud dependency; an Azure
  Content Understanding adapter plugs in behind the same port.
- ✅ **Multi-tenancy** (`AGTec.Tenancy`): a request-scoped `ITenantContext` resolved from
  the route (`tenantId`/workspace) and, when the caller is authenticated, validated
  against the tenant claim — a mismatch is rejected with 403 before the handler runs.
- ✅ **AKS-ready:** chiseled Dockerfiles + Helm chart (`deploy/helm/agtec-reference`)
  with probes, graceful shutdown, secret references, and optional KEDA autoscaling.
- ✅ **Governance & DX:** architecture tests (NetArchTest enforcing the layering in
  CI), a custom **Roslyn analyzer** (`AGTec.Analyzers` — AGTEC0001 flags public setters
  on entities at compile time), a GitHub Actions **CI workflow** (format + audit +
  build in CI mode + Testcontainers tests), and a **`dotnet new agtec-service`**
  template that pre-wires all of the above.

**Tests:** 68 across unit, integration (Testcontainers: Postgres, RabbitMQ,
Elasticsearch, Azurite, pgvector), gRPC, security, malware scanning, tenancy, feature
flags, content extraction, and architecture — all green; CI-mode build (warnings-as-errors) clean.

## Packages

Each package ships its own README (rendered on its NuGet page) as the primary usage doc,
plus XML docs for IntelliSense. Ports live in the core packages; provider adapters are
separate and selected by configuration — no recompile to switch.

| Package | Purpose |
| --- | --- |
| [`AGTec.Domain`](src/AGTec.Domain/README.md) | Domain primitives: `Entity`, `AggregateRoot`, domain events. Zero dependencies. |
| [`AGTec.Application`](src/AGTec.Application/README.md) | CQRS messaging contracts and application-layer conventions. |
| [`AGTec.Messaging`](src/AGTec.Messaging/README.md) | Wolverine-based mediator + transactional outbox + transports (RabbitMQ / Azure Service Bus). |
| [`AGTec.ServiceDefaults`](src/AGTec.ServiceDefaults/README.md) | OpenTelemetry, health checks, service discovery, HTTP resilience for every host. |
| [`AGTec.Api`](src/AGTec.Api/README.md) | API edge: native OpenAPI (JWT bearer scheme), Scalar UI, Problem Details. |
| [`AGTec.Security`](src/AGTec.Security/README.md) | OIDC/JWT bearer authentication + OAuth scope authorization. |
| [`AGTec.Grpc`](src/AGTec.Grpc/README.md) | gRPC server defaults + service-discovery-aware typed clients for internal calls. |
| [`AGTec.Tenancy`](src/AGTec.Tenancy/README.md) | Request-scoped `ITenantContext` from the route, validated against the tenant claim. |
| [`AGTec.FeatureManagement`](src/AGTec.FeatureManagement/README.md) | Feature flags on Microsoft.FeatureManagement with per-tenant targeting (config-driven). |
| [`AGTec.FeatureManagement.Azure`](src/AGTec.FeatureManagement.Azure/README.md) | Azure App Configuration flag source with runtime refresh. |
| [`AGTec.Platform`](src/AGTec.Platform/README.md) | Provider-neutral ports: secrets, object storage, malware scanning, signed URLs (+ in-memory / file-system defaults). |
| [`AGTec.Platform.Azure`](src/AGTec.Platform.Azure/README.md) | Azure adapters: Key Vault secrets + Blob object storage. |
| [`AGTec.Platform.ClamAv`](src/AGTec.Platform.ClamAv/README.md) | `IMalwareScanner` adapter backed by a clamd endpoint. |
| [`AGTec.Platform.Dapr`](src/AGTec.Platform.Dapr/README.md) | `ISecretStore` adapter via the Dapr secrets building block. |
| [`AGTec.Ai`](src/AGTec.Ai/README.md) | No-model defaults on Microsoft.Extensions.AI: deterministic embeddings + a stub chat client. |
| [`AGTec.Ai.OpenAI`](src/AGTec.Ai.OpenAI/README.md) | OpenAI / Azure OpenAI `IChatClient` adapters. |
| [`AGTec.VectorData`](src/AGTec.VectorData/README.md) | Partition-scoped `IVectorStore` port + in-memory default for RAG. |
| [`AGTec.VectorData.PgVector`](src/AGTec.VectorData.PgVector/README.md) | pgvector adapter for `IVectorStore`. |
| [`AGTec.Content`](src/AGTec.Content/README.md) | `ITextExtractor` + paragraph-aware chunking for RAG ingestion. |
| [`AGTec.Content.Local`](src/AGTec.Content.Local/README.md) | Local text extraction adapter: plain text, PDF, Word. |
| [`AGTec.Analyzers`](src/AGTec.Analyzers/README.md) | Roslyn analyzers enforcing domain conventions (e.g. no public setters on entities). |

## Publishing packages

GitHub Actions builds and tests every pull request and push to `main`. Pushing a
semantic-version tag publishes all `AGTec.*` packages to the
[victorgreggio GitHub Packages feed](https://nuget.pkg.github.com/victorgreggio/index.json)
with the tag version:

```bash
git tag v0.1.5
git push origin v0.1.5
```

Tags must use `vMAJOR.MINOR.PATCH`; prerelease and build suffixes are also accepted,
such as `v0.1.5-rc.1`. Publishing uses the workflow's `GITHUB_TOKEN` with
`packages: write`, so no repository secret is required.

## Scaffold a new service

```bash
dotnet new install ./templates/agtec-service
dotnet new agtec-service -n Acme.Weather
```

## Development & conventions

```bash
dotnet tool restore     # CSharpier + Husky.Net (pinned in .config/dotnet-tools.json)
dotnet husky install    # install the pre-commit hook (CSharpier on staged files)
dotnet csharpier format .   # format; CI fails if anything isn't clean
```

- **CSharpier** formatting (`.csharpierrc.json`), enforced by a **Husky pre-commit
  hook** and in CI; mechanical reformats are recorded in `.git-blame-ignore-revs`.
- A **curated analyzer gate** in `.editorconfig` (key correctness `CA` rules are
  build errors; warnings are errors in CI).
- **Renovate** (`renovate.json`) for grouped, scheduled dependency updates.
- **Security** — NuGet vulnerability audit on every build, plus **Mend** SCA + SAST
  (`.github/workflows/mend.yml`). See [`SECURITY.md`](SECURITY.md).

A few items stay deliberately **deferred** (service mesh, per-context event sourcing,
GitOps umbrella charts, internal NuGet feed/governance) — none block v1.
