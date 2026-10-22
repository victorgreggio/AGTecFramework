# AGTecFramework — Design & Architecture Plan

> **Status:** Draft for review · **Version:** 0.3 · **Date:** 2026-10-15
> **Target runtime:** .NET 10 (LTS, supported to Nov 2028) · C# 14
> **Author:** Victor Greggio (draft prepared with Claude Code)

AGTecFramework is a modern, opinionated foundation for building enterprise
microservices. It keeps the patterns that make systems maintainable
— CQRS, Domain-Driven Design, Clean Architecture, event-driven microservices — and
deliberately **challenges every piece of custom infrastructure** against mature,
well-maintained libraries, so that we own our *architecture and conventions*, not a
pile of undifferentiated plumbing.

This document is a **plan, not code.** It states the principles, the target
architecture, the build-vs-buy decisions (with rationale), and a phased roadmap.
The full decision log — all resolved — is in [§14](#14-open-decisions).

**Decisions locked (v0.3):**
- **In-process CQRS + messaging stack → [Wolverine](https://wolverinefx.net/)**
  (MIT): one library for the mediator, pipeline middleware, transactional outbox,
  and broker transport. Directly fixes the lost-event gap. ([§6.1](#61-cqrs), [§6.2](#62-messaging))
- **Primary broker → Azure Service Bus**, consistent with an Azure-leaning infra
  direction — but **pluggable** (Kafka / RabbitMQ) and selected by configuration.
- **Write + document + event store → [Marten](https://martendb.io/) on PostgreSQL**:
  one engine for the write model, document read models, and optional event sourcing;
  pairs natively with Wolverine's outbox. ([§6.4](#64-persistence))
- **Search read model → Elasticsearch / OpenSearch** (self-hostable, portable).
  ([§6.4](#64-persistence))
- **HTTP edge → Minimal APIs** as the single standard; REST at the edge only.
  ([§6.7](#67-api-edge))
- **Cloud posture → Azure-first, not Azure-only.** Every cloud-coupled capability
  is consumed through a provider-neutral abstraction whose implementation is chosen
  by **configuration + restart, never by recompilation** (Level 1 portability).
  ([§8](#8-cloud-strategy), [§8.2](#82-runtime))
- **Deployment → OCI containers on Kubernetes (AKS by default)**, portable to any
  CNCF-conformant cluster. The framework makes services *cluster-friendly by
  default* (probes, graceful shutdown, config/secrets, telemetry). ([§8.5](#85-deployment))
- **Testing → xUnit + AwesomeAssertions** (MIT) + AutoFixture/Bogus + Testcontainers.
  ([§6.8](#68-testing))
- **v1 scope → packages + reference app + AKS-ready templates.** ([§13](#13-roadmap))

---

## Table of contents

1. [Vision & goals](#1-vision--goals)
2. [Patterns we keep & pitfalls we design against](#2-what-we-learned)
3. [Design principles](#3-design-principles)
4. [Target architecture](#4-target-architecture)
5. [The guiding rule: own the architecture, buy the plumbing](#5-build-vs-buy)
6. [Component decisions](#6-component-decisions)
7. [Service-to-service communication (gRPC vs REST vs async)](#7-service-to-service-communication)
8. [Cloud strategy & provider portability (Azure-first, not Azure-only)](#8-cloud-strategy)
9. [Reliability & data-intensive concerns](#9-reliability)
10. [AGTecFramework package layout](#10-package-layout)
11. [Developer experience](#11-developer-experience)
12. [Adoption path](#12-migration-path)
13. [Phased roadmap](#13-roadmap)
14. [Open decisions](#14-open-decisions)
15. [Appendix: books → practice, and references](#15-appendix)

---

## 1. Vision & goals

**Deliver business value faster by making the right thing the easy thing.** A new
service should start from a `dotnet new agtec-service` template, inherit our
standards for observability, messaging, persistence, and testing out of the box,
and let the team spend its time on the *domain*, not on wiring.

Concretely, AGTecFramework should give us:

- **A standardized, opinionated architecture** — every service looks the same at
  the seams (layering, CQRS, messaging contracts, telemetry), so engineers move
  between services with near-zero ramp-up.
- **Decoupled, independently deployable components** that communicate through
  well-defined contracts and, by default, asynchronous events.
- **Reliability by default** — transactional outbox, idempotent consumers,
  retries, dead-lettering, health checks, and tracing are part of the template,
  not an afterthought.
- **Cloud portability** — Azure-first by default, but no business code is bound to
  an Azure SDK; providers are swappable by configuration. ([§8](#8-cloud-strategy))
- **Minimal custom infrastructure.** We write code that is *differentiated* (our
  domain, our conventions, our opinionated glue) and lean on the .NET ecosystem
  for everything that is a solved problem.
- **A living reference application** that proves the framework end-to-end and
  doubles as documentation.

### Non-goals

- Not a general-purpose, "framework for everyone" product. It is opinionated *for
  enterprise microservices*.
- Not a lowest-common-denominator abstraction over every database/broker. We pick
  defaults and support deviation, not infinite pluggability.
- Not event sourcing everywhere. Event sourcing is an option for specific bounded
  contexts, not a global mandate (see [§6.4](#64-persistence)).

---

## 2. Patterns we keep & pitfalls we design against <a id="2-what-we-learned"></a>

AGTecFramework distills hard-won lessons from building hand-rolled .NET
CQRS/microservice stacks. The patterns below are sound and carry forward; the
pitfalls after them are the normal gaps of DIY infrastructure that a mature
ecosystem now fills better — and that AGTecFramework is explicitly designed to
avoid.

### 2.1 Patterns we keep

| Pattern | Why it's right |
|---------|----------------|
| **CQRS split** | Clear separation of commands (write) and queries (read), with dispatchers and handlers. Right mental model. |
| **Clean / Onion layering** | `Domain → Application (use cases) → Infrastructure → API/Worker`. Dependency inversion is respected. |
| **Use-case pattern** | An explicit application-service entry point (one interface per use case) is a clean boundary for the API to call. |
| **Event-driven integration** | Services react to events over a broker; the write side and read side (e.g. document/search stores) are separated. |
| **Service defaults** | A shared *service-defaults* package that centralizes OpenTelemetry, health checks, service discovery, HTTP resilience, API versioning, auth, and CORS — exactly the right idea, and it mirrors Aspire's `ServiceDefaults`. |
| **.NET Aspire** | Aspire AppHost for local orchestration — keep and lean into it. |
| **Specifications & value objects** | DDD tactical building blocks belong in the toolkit. |
| **Logical contract metadata** | A marker that carries a *logical* message name + version (rather than a CLR type) is the right idea for schema evolution. |

### 2.2 Pitfalls we design against

These are concrete anti-patterns — common in DIY CQRS/microservice stacks — that
AGTecFramework is built to avoid.

1. **A hand-rolled mediator that duplicates MediatR, but worse.** Dispatchers that
   merely resolve handlers from `IServiceProvider` tend to have **no pipeline
   behaviors** (validation, logging, transactions, retries done ad hoc), **no
   `CancellationToken`** in the handler signatures, and handlers that implement
   `IDisposable` and dispose their own repositories — a lifetime/ownership smell.

2. **Dispatching messages by reflection over `AssemblyQualifiedName`.** Storing
   `GetType().AssemblyQualifiedName` in the envelope and resolving the handler via
   `Type.GetType(...)` + `MakeGenericMethod(...)` **couples producer and consumer to
   the same CLR type in the same assembly** — the opposite of independently
   deployable microservices, and fragile across versions. (The logical name +
   version is the *correct* routing key; don't override it with an AQN.)

3. **No transactional outbox → the dual-write problem.** Handling a command (DB
   write) and then publishing the integration event on a **fire-and-forget
   background queue** *after* the transaction means a crash between commit and
   publish **loses the event**, and downstream read models silently diverge. This is
   the single most important reliability gap (see *Designing Data-Intensive
   Applications*, Ch. 11–12).

4. **Opening a new broker connection + channel on every publish.** Creating a
   connection inside the publish path is a well-known anti-pattern; connections are
   expensive and meant to be long-lived and shared, with channels pooled.

5. **Poison-message handling as a redelivery loop.** Nacking with *requeue* on
   failure, with no retry policy, no backoff, no dead-letter queue, and no
   idempotency/dedup, makes a single bad message loop forever.

6. **Conflating domain events with integration events.** A single "raise event"
   path that *always* serializes and publishes to the bus has no notion of an
   in-process **domain event** raised by an aggregate inside its transaction,
   distinct from an **integration event** crossing a service boundary.

7. **Anemic domain model.** Entities that derive from a value-object base, expose
   `Id`/audit/concurrency fields with **public setters**, and hold no behavior are
   pure data bags. Invariants then live in handlers, not the model — contrary to the
   DDD the architecture claims to follow.

8. **A generic repository that leaks `IQueryable`.** Returning `IQueryable<TEntity>`
   from the repository pushes EF Core concerns into the application layer, and
   repository-per-call with a `saveImmediately` flag signals a missing unit-of-work
   boundary. EF Core's `DbContext` *is already* a unit of work + repository.

9. **"Services" that share a database.** A read-model projector that re-queries the
   producer's own database is a modular monolith deployed as separate processes, not
   autonomous microservices (each owning its data). We want the boundary to be
   explicit: integration happens through events/APIs, not a shared schema.

10. **Bespoke utilities that libraries do better.** A custom random/test-data
    generator overlaps AutoFixture/Bogus; a custom background-task queue overlaps
    `System.Threading.Channels` + hosted services; a reflection-based value-object
    base class is largely obsoleted by C# `record` types.

11. **Reaching for dependencies that have since gone commercial.** **AutoMapper**
    (dual-licensed from v15), **MediatR** (dual-licensed from v13), and
    **MassTransit** (commercial from v9) are the reflexive choices — but their
    licensing now materially changes build-vs-buy for an enterprise. See
    [§5](#5-build-vs-buy).

12. **Smaller items.** Controllers wrapping every action in `try/catch`+rethrow
    (should be one global exception handler / `IExceptionHandler`); registering
    `Swashbuckle` *and* native `Microsoft.AspNetCore.OpenApi` (redundant); targeting
    an out-of-date runtime; `Thread.CurrentPrincipal?.Identity?.Name` for the current
    user (should be `IHttpContextAccessor` / an ambient `ICurrentUser` abstraction).

---

## 3. Design principles

1. **Own the architecture, buy the plumbing.** Our value is in conventions and the
   domain, not in a bespoke message bus. ([§5](#5-build-vs-buy))
2. **Asynchronous & event-driven by default; synchronous by exception.**
   Temporal decoupling is the default integration style. ([§7](#7-service-to-service-communication))
3. **Contracts are explicit, versioned, and technology-neutral.** Messages and
   service contracts are first-class, named, versioned artifacts — never a leaked
   CLR type. ([§6.2](#62-messaging), [§7](#7-service-to-service-communication))
4. **Each service owns its data.** No shared databases across bounded contexts.
   Integration happens through events/APIs, not a shared schema.
5. **Rich domain model.** Behavior and invariants live in aggregates; application
   services orchestrate, they don't *be* the model. ([§6.3](#63-domain-model))
6. **Reliability is designed-in, not bolted-on.** Outbox, idempotency, retries,
   dead-lettering, health, and tracing ship in the template. ([§9](#9-reliability))
7. **Observable by default.** Every service emits traces, metrics, and structured
   logs with correlation propagated across process and broker boundaries.
8. **Cloud-portable: Azure-first, not Azure-only.** Default to managed Azure
   services, but no domain/application code depends on a cloud SDK. Provider-coupled
   capabilities sit behind abstractions selected by **configuration at runtime, not
   by recompilation**. ([§8](#8-cloud-strategy))
9. **Convention over configuration, with escape hatches.** Opinionated defaults;
   deviation is possible and explicit.
10. **Testable by construction.** Pure domain, injected ports, and
    container-backed integration tests. ([§6.8](#68-testing))
11. **Favor the platform.** Prefer first-party .NET/Microsoft and permissively
    licensed OSS over DIY or commercial lock-in.

---

## 4. Target architecture

### 4.1 Layers (per service)

Clean / Onion architecture, dependencies pointing inward only:

```
┌──────────────────────────────────────────────────────────────┐
│  Host / Edge                                                   │
│  - API (REST/JSON at the edge) and/or gRPC endpoints           │
│  - Worker (message consumers, background jobs)                 │
│  - Composition root (DI), AGTecFramework ServiceDefaults        │
└───────────────┬────────────────────────────────────────────────┘
                │ depends on
┌───────────────▼────────────────────────────────────────────────┐
│  Application  (use cases, CQRS)                                 │
│  - Commands / Command handlers   - Queries / Query handlers     │
│  - Integration-event handlers    - Ports (interfaces)           │
│  - Pipeline behaviors: validation, logging, tx, idempotency     │
└───────────────┬────────────────────────────────────────────────┘
                │ depends on
┌───────────────▼────────────────────────────────────────────────┐
│  Domain  (the core, no external dependencies)                   │
│  - Aggregates, Entities, Value Objects                          │
│  - Domain events, Domain services, Specifications               │
│  - Invariants & business rules                                  │
└──────────────────────────────────────────────────────────────┘
                ▲ implemented by (dependency inversion)
┌───────────────┴────────────────────────────────────────────────┐
│  Infrastructure  (adapters)                                     │
│  - Persistence (EF Core write model, read stores)               │
│  - Messaging (broker transport + outbox/inbox)                  │
│  - Cloud adapters (storage, secrets, cache) behind ports        │
│  - External service clients (gRPC/HTTP), etc.                   │
└──────────────────────────────────────────────────────────────┘
```

**Rule enforced by architecture tests:** `Domain` references nothing;
`Application` references `Domain`; `Infrastructure` and `Host` reference inward;
`Domain`/`Application` never reference EF Core, the broker SDK, a cloud SDK, ASP.NET,
etc. The only things that touch a cloud SDK are the **adapter** packages. ([§8](#8-cloud-strategy))

### 4.2 Reference topology

```
                         ┌─────────────────────────┐
     external clients →  │  Edge: API Gateway (YARP)│  REST/JSON, OIDC/JWT
     (browser, mobile)   └────────────┬────────────┘
                                      │ REST at the edge
                         ┌────────────▼────────────┐
                         │   Service A: WebApi       │  writes via commands
                         │   (PostgreSQL + Marten)   │  → Outbox → broker
                         └───────┬────────┬─────────┘
                  gRPC (sync,    │        │  publishes integration events
                  when required) │        ▼
                         ┌───────▼──┐  ┌────────────────────┐
                         │ Service B│  │ Broker (config-sel.)│ Azure Service Bus
                         │  (gRPC)  │  │  + DLQ              │ (default) / Kafka /
                         └──────────┘  └──────┬──────────────┘ RabbitMQ (local)
                                              │ at-least-once, idempotent
                                   ┌──────────▼───────────┐
                                   │ Worker: read-model    │  owns its read
                                   │ projector (consumer)  │  stores
                                   └──────┬────────┬───────┘
                                          ▼        ▼
                              Marten/Postgres   Elasticsearch
                              (document read)   / OpenSearch (search)
```

Key differences from today:
- Integration events flow through a **transactional outbox** (Wolverine), not a
  fire-and-forget queue.
- The broker is **selected by configuration** — Azure Service Bus in the cloud,
  RabbitMQ locally — with no code change. ([§8](#8-cloud-strategy))
- The projector **does not read the producer's database**; the event carries the
  data it needs (or the projector keeps its own copy). Services are data-autonomous.
- Internal **synchronous** calls (rare) use **gRPC**; the **edge** stays REST/JSON.

### 4.3 Bounded contexts

A "service" maps to a **bounded context** (DDD, Evans). Context boundaries — not
technical layers — are the unit of autonomy and deployment (Newman, *Building
Microservices*). The framework is deliberately silent on how many services you
have; it makes each one cheap and consistent to build. Start as a **modular
monolith** and extract services along context seams when load, team, or release
cadence justifies it.

---

## 5. The guiding rule: own the architecture, buy the plumbing <a id="5-build-vs-buy"></a>

The original brief — *"challenge the existing code and see if there is any library
that would do a better job than our own implementation"* — is the heart of this
project. Our decision rule:

> **Build** the thin, opinionated layer that encodes *our* conventions and domain.
> **Buy / adopt** anything that is a solved, commoditized problem and is better
> maintained by someone else. **Wrap** a bought component behind our own small
> interface *only* when we genuinely expect to swap it — which, for cloud-coupled
> resources, we explicitly do ([§8](#8-cloud-strategy)). Even then, **prefer an
> existing standard abstraction** (OpenTelemetry, `IDistributedCache`, OIDC, EF
> Core, Wolverine transports) over a bespoke AGTec interface.

### 5.1 The 2025 licensing shift (why this is urgent now)

Several libraries that a .NET team would reflexively reach for went **commercial
in 2025**. This is decisive for enterprise teams, which will almost
certainly exceed the free "Community" revenue thresholds:

| Library | Change | Free fallback |
|---------|--------|---------------|
| **MediatR** | Dual-licensed from **v13.0** (Jul 2025). Community edition free only under **$5M** gross annual revenue. v12.5.0 is the last plain Apache-2.0 release. | **Wolverine** (chosen), or source-gen **Mediator**, or a thin hand-rolled mediator. |
| **AutoMapper** | Dual-licensed from **v15.0** (Jul 2025). Same revenue threshold. v14.x stays MIT forever (no updates). | **Mapperly** (source-gen, MIT) or hand-written mapping. |
| **MassTransit** | **v9 commercial** (Massient, Inc., Nov 2025). **v8 stays open source** (Apache-2.0) with security patches planned through 2026. | **Wolverine** (chosen, MIT), MassTransit **v8**, or NServiceBus (also commercial). |

> ⚠️ Pricing/terms move quickly — **verify against each vendor's current license
> page before committing.** The figures circulating in blogs are unofficial.

The silver lining: the free, permissively licensed alternatives are **genuinely
excellent**, and this is exactly the forcing function to stop hand-rolling and
adopt them deliberately. We chose **Wolverine** (MIT) partly for this reason.

### 5.2 Summary decision table

| Concern | Typical DIY approach | AGTecFramework decision | Why |
|---------|---------------|------------------------|-----|
| In-process CQRS mediator | Custom dispatchers | **Wolverine** (mediator + middleware) | MIT; pipeline behaviors + cancellation; collapses with messaging/outbox |
| Messaging + outbox | Custom RabbitMQ | **Wolverine** transports + built-in outbox/inbox | Mature, retries, DLQ, sagas; transport chosen by config |
| Primary broker | RabbitMQ | **Azure Service Bus** (default), RabbitMQ (local), Kafka (option) — config-selected | Azure-first infra direction; portable ([§8](#8-cloud-strategy)) |
| Object mapping | AutoMapper | **Mapperly** (source-gen, MIT) | Compile-time, fast, no license, no runtime reflection |
| Validation | `InvalidModelStateFilter` | **FluentValidation** in a pipeline behavior | Standard, expressive, testable |
| Write persistence | Generic repo over EF Core | **EF Core directly** (DbContext = UoW) + repo *per aggregate* returning domain types | Stop leaking `IQueryable`; clear tx boundary |
| Document/event store | Mongo repo | **Marten** on PostgreSQL (document read models + optional event sourcing) | One engine; fewer moving parts; native Wolverine outbox |
| Search read model | ES repo | **Elasticsearch / OpenSearch** via projector | Portable, self-hostable, richest relevance |
| Cloud-coupled resources | (implicit) | **Ports + config-selected adapters**, Azure-first | Portability without recompilation ([§8](#8-cloud-strategy)) |
| Background work | Custom `BackgroundTaskQueue` | `System.Threading.Channels` + hosted service, or the broker | First-party, less code |
| Test data | Custom `Randomizer` | **AutoFixture** + **Bogus** | Richer, maintained |
| Service defaults | Custom service-defaults library | **`AGTec.ServiceDefaults`** on Aspire ServiceDefaults | This is our differentiator — invest here |
| Local orchestration | .NET Aspire | **Keep & lean in** | Best-in-class DX |
| Edge API docs | Swashbuckle + OpenAPI (both) | **`Microsoft.AspNetCore.OpenApi`** + Scalar UI | Native in .NET 10, drop redundancy |
| Sync service-to-service | (WebAPI) | **gRPC** | See [§7](#7-service-to-service-communication) |

The rest of [§6](#6-component-decisions) explains the non-obvious ones.

---

## 6. Component decisions <a id="6-component-decisions"></a>

### 6.1 CQRS / in-process mediator <a id="61-cqrs"></a>

**Decision: adopt [Wolverine](https://wolverinefx.net/) (MIT) as both the
in-process mediator and the messaging bus.** Wolverine gives us local command
handling, an ordered middleware pipeline, the transactional outbox, and broker
transports in one coherent model — collapsing [§6.1](#61-cqrs) and
[§6.2](#62-messaging) into a single dependency and removing the most custom code.

**Keep** the command/query/handler model and the use-case entry points. **Fix**
the mechanics, which Wolverine supports directly:

- **Pipeline middleware** (the single biggest missing feature): cross-cutting
  concerns run as an ordered pipeline around every handler —
  `Logging → Validation → Transaction/UnitOfWork → Idempotency → Handler`.
- **`CancellationToken` everywhere.** Every handler receives one.
- **Commands may return results**, not only `void`.
- **Handlers are not `IDisposable`.** Lifetime is owned by the DI scope; a handler
  never disposes a repository.
- **Dispatch is separate from transport.** Local handling vs publishing to the bus
  is explicit (via the outbox), not a hidden branch keyed off a `[Publishable]`
  attribute as it is today.

> **Fallback if Wolverine's conventions don't fit:** a ~200-line source-generated
> AGTec mediator we own (keeps the application core dependency-free), paired with a
> separate messaging library. Kept as a documented escape hatch, not the plan.

### 6.2 Messaging <a id="62-messaging"></a>

**Decision: Wolverine transports with Azure Service Bus as the default broker**,
RabbitMQ for local/dev, Kafka as an option for high-throughput streaming — the
active transport is **chosen by configuration** ([§8](#8-cloud-strategy)). We do
*not* re-wrap Wolverine behind a AGTec publish interface; Wolverine's transport
model *is* the abstraction. Target capabilities:

- **Long-lived, pooled connections** (fixes the connect-per-publish anti-pattern).
- **Transactional outbox**: integration events are written to an outbox table **in
  the same transaction** as the state change, then relayed to the broker by a
  background dispatcher. Fixes the dual-write/lost-event problem. (Wolverine's
  outbox works with EF Core and with Marten.)
- **Inbox / idempotent consumers**: consumers dedupe on message id so
  at-least-once delivery is safe to process exactly-once-effectively.
- **Retries with backoff + dead-letter queues** for poison messages (no infinite
  requeue loops).
- **Technology-neutral, versioned contracts**: route and deserialize by a **stable
  logical message name + version** (we already have `Label`/`Version`), **never**
  by `AssemblyQualifiedName`. Producer and consumer share a *contracts package*, not
  an implementation assembly.
- **Sagas / process managers** for multi-step distributed workflows.
- **Correlation**: propagate W3C `traceparent` through message headers so a trace
  spans producer → broker → consumer.

**Domain events vs integration events (make the distinction first-class):**

| | Domain event | Integration event |
|-|--------------|-------------------|
| Scope | Inside one aggregate/service | Across service boundaries |
| Transport | In-process (mediator), same transaction | Broker, via outbox |
| Coupling | Internal types OK | Versioned, published contract only |
| Example | `OrderLineAdded` | `OrderPlaced` v1 |

### 6.3 Domain model & DDD tactical patterns <a id="63-domain-model"></a>

Move from anemic to **rich** domain models:

- **Aggregate roots** encapsulate invariants; state changes go through methods, not
  public setters. Introduce `AggregateRoot` (an `Entity` that can raise **domain
  events**) and a clear aggregate boundary.
- **Entities** keep identity-based equality; drop the `Entity : ValueObject`
  inheritance. `Id` is set once (constructor/factory), not publicly mutable.
- **Value objects**: prefer C# `record` / `readonly record struct` (free
  structural equality) over the reflection-based `ValueObject` base. Keep a small
  base only where a `record` won't do.
- **Audit fields** (`Created`, `LastUpdated`, `UpdatedBy`, concurrency `Version`)
  move to an EF Core interceptor / base `AggregateRoot`, not hand-set in handlers.
- **Specifications**: keep the pattern, but compile LINQ expressions once (cache)
  and let the write-side repository translate a specification to an EF query.
- **Current user**: an injected `ICurrentUser` port (backed by
  `IHttpContextAccessor` at the edge, message headers in the worker) replaces
  `Thread.CurrentPrincipal`.

References: Evans (*DDD*) and Vernon (*Implementing DDD*) for aggregates, invariants,
and the domain-event pattern.

### 6.4 Persistence <a id="64-persistence"></a>

**Decision: standardize on PostgreSQL, with [Marten](https://martendb.io/) (JasperFx,
MIT) as the document + event store.** Marten + Wolverine is the "critical stack":
one Postgres engine serves the write model, document read models, the transactional
outbox, and optional event sourcing — a large reduction in moving parts.

- **Write side:** aggregates persisted via Marten's session (its document session
  *is* the unit of work) or EF Core where a relational schema fits better. A
  **repository per aggregate root** exposes intention-revealing methods returning
  **domain objects** (not `IQueryable`). Wolverine's transaction middleware commits
  once per command and flushes the outbox in the same transaction.
- **Event sourcing (optional, per context):** Marten provides it natively where a
  bounded context benefits (full audit, temporal queries, projections). Not a global
  mandate — most contexts stay state-based.
- **Read side (CQRS):** thin read models populated by projectors —
  **Marten documents/projections** for the default document read model, **Dapper**
  for hot relational read paths.
- **Search read model → Elasticsearch / OpenSearch**, fed by the projector; chosen
  for portability (self-hostable on AKS or managed) and relevance features.
- **Outbox** lives in the same Postgres database as the write model for
  transactional consistency (Marten-backed).
- **Relational engine = PostgreSQL** for cloud portability (runs identically on
  Azure Database for PostgreSQL and self-hosted). See the portability caveat in
  [§8.4](#84-caveats).
- **MongoDB remains an *alternative* document adapter** behind the read-model port
  for a context that specifically needs it — not the default.
- **Schema/migrations:** Marten manages its own schema (dev auto-create, production
  migration scripts); EF Core migrations where EF is used. Run as an explicit
  startup/CI step (Aspire can gate service start on DB readiness).

### 6.5 Validation & mapping

- **Validation:** **FluentValidation**, executed in a `ValidationBehavior` pipeline
  step so commands/queries are validated before the handler runs. API model-state
  validation stays at the edge as a fast fail.
- **Mapping:** **Mapperly** (source-generated, MIT) replaces AutoMapper — compile-time
  safety, no runtime reflection, no license question. For trivial cases, prefer
  explicit hand-written mapping; mapping is domain logic worth being visible.

### 6.6 Observability & resilience <a id="66-observability--resilience"></a>

- **Keep OpenTelemetry** (traces, metrics, logs) from ServiceDefaults; ensure
  context propagates across HTTP, gRPC, *and* the broker. OTLP is the vendor-neutral
  sink abstraction — the exporter (Azure Monitor by default) is a config choice
  ([§8](#8-cloud-strategy)).
- **Structured logging** standardized (Serilog or `Microsoft.Extensions.Logging`
  with OTLP export). Correlation/causation ids on every log line and message.
- **Resilience:** `Microsoft.Extensions.Http.Resilience` (Polly v8) for outbound
  HTTP/gRPC — already in ServiceDefaults; formalize standard policies (timeout,
  retry, circuit breaker, hedging).
- **Health checks:** liveness vs readiness split (plus startup), mapped to
  Kubernetes probes; readiness gates on broker + DB. ([§8.5](#85-deployment))

### 6.7 API edge <a id="67-api-edge"></a>

- **Edge protocol:** REST/JSON for external clients (browser, mobile, partners).
  **Decision: Minimal APIs** (with endpoint filters) are the single standard — a
  thin `IEndpoint` convention per feature, dispatching straight to the Wolverine
  bus. Controllers are a documented exception only where complex model binding
  genuinely warrants them.
- **Versioning:** keep **Asp.Versioning** (URL segment + header).
- **OpenAPI:** standardize on **`Microsoft.AspNetCore.OpenApi`** (native in .NET
  9/10) with **Scalar** or Swagger UI — drop the double registration of Swashbuckle
  *and* native OpenAPI.
- **Error handling:** one `IExceptionHandler` + **RFC 9457 Problem Details**;
  remove per-action `try/catch`+rethrow.
- **Gateway:** keep **YARP** at the edge for routing/transform/rate-limiting.
- **AuthN/Z:** keep OIDC/JWT and the custom claim/scope authorization — it is a
  genuine, reusable asset; move it into `AGTec.Security`. OIDC keeps us identity-provider
  portable (Entra ID default; any OIDC IdP otherwise).

### 6.8 Testing <a id="68-testing"></a>

- **Unit:** **xUnit** + **AwesomeAssertions** (MIT fork of the FluentAssertions v7
  API — same fluent syntax, no commercial license) + **NSubstitute/Moq**.
- **Test data:** **AutoFixture** + **Bogus** (no bespoke randomizer).
- **Integration:** **Testcontainers** to spin up PostgreSQL / the broker / ES
  in tests — real infrastructure, disposable.
- **Architecture tests:** **NetArchTest** / **ArchUnitNET** to enforce the layering
  rules in [§4.1](#41-layers-per-service) in CI — including "no cloud SDK outside
  adapter packages".
- **Contract tests:** **Pact** (consumer-driven) and/or shared `.proto`/message
  contract packages verified in CI, so services evolve independently without
  breaking consumers.

---

## 7. Service-to-service communication (gRPC vs REST vs async) <a id="7-service-to-service-communication"></a>

You asked specifically whether service-to-service should move off WebAPI/JSON onto
gRPC. The answer is **a layered default**, drawn from Newman (*Building
Microservices*, 2nd ed., Ch. 4–5):

### 7.1 Decision matrix

| Situation | Use | Rationale |
|-----------|-----|-----------|
| **Propagating state / facts between services** (the common case) | **Asynchronous events** (broker + outbox) | Temporal decoupling, resilience, independent scaling/availability. **This is the default.** |
| **Internal synchronous request/response** that genuinely can't be async (e.g., a real-time lookup during a request) | **gRPC** (HTTP/2, Protobuf, contract-first) | Lower latency & payload size than JSON, strongly-typed generated clients, streaming, first-class in .NET/Aspire |
| **External / edge / browser-facing APIs** | **REST/JSON** (optionally GraphQL for aggregation) | Universally consumable, human-debuggable, caching, ecosystem |
| **Fan-out / event streaming / high-throughput log** | **Kafka** (or Azure Event Hubs) | Durable log, replay, high throughput |

### 7.2 Recommendation

- **Default to asynchronous messaging** for integration. Most "service A needs
  something from service B" problems are better modeled as B publishing facts that
  A keeps a local copy of, rather than A calling B at request time.
- **Adopt gRPC for internal synchronous calls**, replacing internal WebAPI/JSON.
  Contracts live in **`.proto`** files in a shared, versioned **contracts package**;
  generated clients get the same resilience handlers as HTTP. gRPC is a first-class
  citizen in .NET 10 and Aspire.
- **Keep REST/JSON at the edge** only. Browsers can't speak raw gRPC
  (needs gRPC-Web/a proxy), and external consumers expect REST.
- **Avoid synchronous call chains.** A request that fans out through three
  synchronous hops multiplies latency and failure probability (Newman's "don't
  build a distributed monolith"). Prefer choreography via events; use **sagas** for
  coordinated multi-service workflows.

> Net: the demo's instinct (events over the broker) is right and stays the default.
> We **add gRPC for the genuinely synchronous internal calls** and stop using
> WebAPI/JSON between services.

---

## 8. Cloud strategy & provider portability (Azure-first, not Azure-only) <a id="8-cloud-strategy"></a>

**Principle:** We deliberately target **managed Azure services as our defaults**,
but **no domain or application code depends on an Azure SDK**. Every cloud-coupled
capability is consumed through a provider-neutral **port** (abstract interface);
provider-specific **adapters** implement the port and live in separate packages;
the **active adapter is chosen by configuration and wired by DI at the composition
root**. Swapping a provider is a configuration + deployment change — **never a
source-code change and never conditional compilation**.

### 8.1 Mechanism — Ports & Adapters (Hexagonal) <a id="81-mechanism"></a>

1. **Ports** live in provider-neutral packages (`AGTec.*.Abstractions`) with **zero
   provider SDK references**. The application/domain depend only on ports.
2. **Adapters** live in `AGTec.*.<Provider>` packages that reference the vendor SDK.
   The adapter is the *only* place a cloud SDK appears (enforced by architecture
   tests — [§6.8](#68-testing)).
3. **Selection** happens at the composition root from configuration, e.g.:

   ```jsonc
   // appsettings.json — switch providers with config only, no recompile
   "AGTec": {
     "Messaging": { "Provider": "AzureServiceBus" },  // or "RabbitMq", "Kafka"
     "Secrets":   { "Provider": "AzureKeyVault"    },  // or "Vault", "Env"
     "ObjectStore": { "Provider": "AzureBlob"      }   // or "S3", "MinIO"
   }
   ```

   A small provider registry (`services.AddAGTecMessaging(config)`) reads the key
   and registers the matching adapter. Provider-specific settings bind via the
   options pattern. No `#if`, no provider type names in business code.
4. **Prefer existing standard abstractions as the port** where one exists —
   OpenTelemetry/OTLP (telemetry), `IDistributedCache` (caching), OIDC
   (identity), EF Core (relational), Wolverine transports (messaging). Only define
   a bespoke AGTec port where no good standard exists (e.g., object/blob store,
   secret store). This keeps us consistent with [§5](#5-build-vs-buy) — we don't
   reinvent abstractions the platform already provides.

### 8.2 What "runtime, not compiling" means here <a id="82-runtime"></a>

Two levels — **decision: we target Level 1.**

- **Level 1 — config-selected at startup (our target).** The deployed binary can
  carry multiple adapters; configuration selects the active one; switching a
  provider is a **config change + restart**, with **no recompilation and no code
  change**. This satisfies the "runtime, not compiling" requirement for essentially
  every real scenario and is how cloud-portable systems are normally built.
- **Level 2 — hot-swap in a live process** (change provider without restart) is a
  much higher bar (connection draining, dual-running, in-flight message handling),
  rarely justified, and **explicitly out of scope** unless a specific requirement
  appears later.

### 8.3 Capability → port → Azure default → alternatives <a id="83-matrix"></a>

| Capability | Port / abstraction | Azure-first adapter | Alternative adapters | Notes |
|---|---|---|---|---|
| Async messaging / events | Wolverine transport (config) | **Azure Service Bus** | RabbitMQ, Kafka, Amazon SQS | Transport chosen by config; outbox stays DB-based & provider-neutral |
| Secrets | `ISecretStore` (AGTec) + config provider | **Azure Key Vault** | HashiCorp Vault, AWS Secrets Manager, env/file | |
| App configuration | .NET config providers | **Azure App Configuration** | Consul, file/env, k8s ConfigMap | Feature flags via the same provider |
| Object / blob storage | `IObjectStore` (AGTec) | **Azure Blob Storage** | AWS S3, GCS, MinIO (S3-compatible) | Stream-based, SAS/URL helpers behind the port |
| Distributed cache | `IDistributedCache` (MS) | **Azure Cache for Redis** | Redis/Valkey anywhere, in-memory | Already a standard abstraction |
| Relational database | EF Core `DbContext` | **Azure Database for PostgreSQL** | PostgreSQL anywhere; other EF providers | *Partial* portability — see [§8.4](#84-caveats) |
| Document / event store | Marten (PostgreSQL) | **Azure Database for PostgreSQL** (+ Marten) | PostgreSQL anywhere; MongoDB adapter if a context needs it | Default store; same engine as the write model |
| Search | read-model port | **Elasticsearch / OpenSearch** (self-hosted on AKS or Elastic Cloud) | Azure AI Search, OpenSearch anywhere | Fed by projector; default is portable ES/OpenSearch |
| Identity | OIDC / JWT; `ICurrentUser` | **Microsoft Entra ID** | Keycloak, Duende, Auth0 — any OIDC IdP | Standard OIDC ⇒ portable |
| Telemetry sink | OpenTelemetry (OTLP) | **Azure Monitor / App Insights** | Grafana/Tempo/Prometheus, Jaeger, Datadog | Exporter is a config choice |
| Email / notifications | `INotificationSender` (AGTec) | Azure Communication Services | SendGrid, Amazon SES | As needed |
| Container hosting | (platform, not in-code) | **Azure Kubernetes Service (AKS)** | Any CNCF Kubernetes, on-prem, ECS | k8s is the portable compute layer; images OCI-standard; no code coupling ([§8.5](#85-deployment)) |

### 8.4 Honest caveats <a id="84-caveats"></a>

- **Relational provider is the least runtime-swappable.** EF Core provider packages
  (Npgsql vs SqlServer) are referenced at build and differ in SQL dialect; "swap at
  runtime" here means *"the same binary runs against Azure Database for PostgreSQL
  or self-hosted PostgreSQL,"* **not** *"switch PostgreSQL ↔ SQL Server without
  retesting."* We standardize on **PostgreSQL** and treat the *hosting* as the
  portable part.
- **Don't over-abstract.** Wrapping is justified for cloud-coupled resources because
  we genuinely expect to swap them — but where a vendor-neutral standard already
  exists, use it ([§8.1](#81-mechanism) rule 4) rather than inventing a AGTec
  interface.
- **Isolate vendor-only features.** If a bounded context must use an Azure-only
  capability, it lives inside the adapter with a documented fallback for other
  providers, so the leak is contained and visible.
- **Cost of portability is real** (extra abstraction, adapter maintenance, testing
  each adapter). We pay it only for capabilities we realistically might move; Azure
  remains the first-class, best-tested path.

### 8.5 Deployment & runtime — containers on Kubernetes (AKS) <a id="85-deployment"></a>

Our deployment target is **OCI containers on Kubernetes, AKS by default.** This
*reinforces* the Azure-first/portable stance: Kubernetes is the portable compute
layer, so the platform moves across clouds (or on-prem) even though AKS is the
managed default. The framework's job is to make every service **cluster-friendly by
default**; owning the cluster itself is a platform/ops concern, intentionally thin
in the framework.

Baked into the templates:

- **Container images:** .NET 10 **chiseled / distroless** base images (small,
  rootless, fewer CVEs), multi-stage builds, native AOT where it pays (gRPC,
  workers). One image per service; configuration injected at runtime.
- **Config & secrets, 12-factor:** configuration from env vars / ConfigMaps;
  secrets from Kubernetes Secrets or, preferably, **Azure Key Vault via the Secrets
  Store CSI driver + Workload Identity** — the same config-selected `ISecretStore`
  story as [§8.1](#81-mechanism), portable to other secret backends.
- **Health probes:** the liveness/readiness split ([§6.6](#66-observability--resilience))
  maps directly to k8s `livenessProbe` / `readinessProbe` (readiness gates on broker
  + DB so a pod takes traffic only when its dependencies are ready), plus a
  `startupProbe` for slow cold starts.
- **Graceful shutdown:** handle `SIGTERM` via `IHostApplicationLifetime`; Wolverine
  message consumers **drain in-flight work** before exit so rolling deploys don't
  drop messages. Tune `terminationGracePeriodSeconds` to match.
- **Autoscaling:** HPA on CPU/memory for request services; **KEDA** for workers —
  scale consumers on **Azure Service Bus queue/topic depth** (or Kafka lag) and
  scale to zero when idle. A natural fit with the async messaging design.
- **Networking:** REST + gRPC exposed via ingress/gateway (YARP or an ingress
  controller); service-to-service over cluster DNS; mTLS / service mesh optional
  ([§14](#14-open-decisions)).
- **Local ⇄ cluster parity:** Aspire for local dev; the *same images + Helm charts*
  for AKS. Provider defaults differ **by config only** (RabbitMQ + PostgreSQL
  locally → Azure Service Bus + Azure Database for PostgreSQL in the cloud), never
  by code.
- **Delivery:** Helm charts (or Kustomize) per service shipped as templates;
  GitOps-friendly (Flux / Argo CD); CI builds and pushes images to **ACR**.

---

## 9. Reliability & data-intensive concerns <a id="9-reliability"></a>

Informed by Kleppmann (*Designing Data-Intensive Applications*) and Newman:

- **Dual writes are forbidden.** Any "write DB then publish" is replaced by the
  **transactional outbox**. ([§6.2](#62-messaging))
- **At-least-once + idempotency.** Assume every message can be delivered more than
  once; consumers are **idempotent** (inbox/dedup). Exactly-once *delivery* is a
  myth; exactly-once *effect* is achievable via idempotency.
- **Eventual consistency is explicit.** Read models lag the write model; the
  framework and docs make this a stated contract, with projectors that can be
  **rebuilt/replayed** from the source of truth.
- **Schema/versioning.** Messages are versioned (`v1`, `v2`), backward/forward
  compatible where possible; never route by CLR type. Protobuf (gRPC) and
  explicit JSON contracts both support additive evolution.
- **Failure handling.** Retries with backoff, circuit breakers, timeouts,
  bulkheads (Polly); dead-letter queues + alerting for poison messages;
  compensating actions via sagas for distributed transactions (no 2PC).
- **Backpressure.** Prefetch limits and bounded channels so a slow consumer
  doesn't exhaust memory.
- **Observability of data flow.** Trace a business transaction end-to-end across
  services and the broker; expose lag/queue-depth metrics.

---

## 10. AGTecFramework package layout <a id="10-package-layout"></a>

Opinionated, small, composable NuGet packages (names illustrative). Provider
adapters are **separate packages** so a host references only the adapters it ships
with and selects the active one by configuration ([§8](#8-cloud-strategy)).

**Core (provider-neutral, no cloud/broker SDKs):**

| Package | Contents |
|---------|----------|
| `AGTec.Domain` | `AggregateRoot`, `Entity`, value-object helpers, domain events, specifications. **Zero dependencies.** |
| `AGTec.Application` | CQRS abstractions, pipeline behaviors (validation, logging, transaction, idempotency), use-case conventions (over Wolverine). |
| `AGTec.Abstractions` | Provider-neutral **ports**: `ISecretStore`, `IObjectStore`, `INotificationSender`, `ICurrentUser`, etc. No SDK references. |
| `AGTec.Messaging` | Integration-event contract conventions, outbox config, and config-driven transport selection over Wolverine. |
| `AGTec.Persistence.Marten` | Marten/PostgreSQL conventions: aggregate sessions, document read models, outbox wiring, optional event sourcing, migration hooks. **Default store.** |
| `AGTec.Persistence.EfCore` | EF Core `DbContext` conventions, audit interceptor, repository-per-aggregate base — for contexts that prefer a relational schema. |
| `AGTec.ReadModel` | Projector conventions for Elasticsearch/OpenSearch (search) and Marten document read models. |
| `AGTec.Grpc` | gRPC server/client conventions, resilience, interceptors (telemetry, auth). |
| `AGTec.Api` | Minimal-API endpoint conventions, Problem Details, versioning, OpenAPI. |
| `AGTec.Security` | OIDC/JWT, reusable claim/scope authorization handlers. |
| `AGTec.ServiceDefaults` | The crown jewel: OpenTelemetry, health, service discovery, resilience, logging — composed for API, Worker, and gRPC hosts. Built on Aspire's ServiceDefaults. |
| `AGTec.Testing` | Base test classes (xUnit), AwesomeAssertions + AutoFixture/Bogus customizations, Testcontainers fixtures, architecture-test rules. |
| `AGTec.Templates` | `dotnet new` templates (see [§11](#11-developer-experience)). |

**Provider adapters (reference a vendor SDK; chosen by config):**

| Package | Implements |
|---------|-----------|
| `AGTec.Messaging.AzureServiceBus` · `AGTec.Messaging.RabbitMq` · `AGTec.Messaging.Kafka` | messaging transport |
| `AGTec.Secrets.AzureKeyVault` · `AGTec.Secrets.Vault` · `AGTec.Secrets.Env` | `ISecretStore` |
| `AGTec.Storage.AzureBlob` · `AGTec.Storage.S3` | `IObjectStore` |
| `AGTec.Search.Elasticsearch` · `AGTec.Search.AzureAiSearch` | search read model |
| `AGTec.Persistence.Mongo` *(optional)* | MongoDB document adapter (non-default) |
| `AGTec.Telemetry.AzureMonitor` *(exporter config)* | OTLP → Azure Monitor |

Principle: a service references only what it needs; `AGTec.Domain`,
`AGTec.Application`, and `AGTec.Abstractions` stay pristine and cloud-agnostic.

---

## 11. Developer experience <a id="11-developer-experience"></a>

The framework's payoff is **speed with consistency**. Invest in:

- **`dotnet new` templates**: `agtec-service` (API + Application + Domain +
  Infrastructure + tests, wired for CQRS/outbox/telemetry), `agtec-worker`
  (consumer/projector), `agtec-grpc`. A new, fully-wired service in minutes.
- **Aspire AppHost template** for local orchestration (DB, broker, search,
  dashboard); local defaults use RabbitMQ +
  PostgreSQL, cloud uses Azure Service Bus + Azure Database for PostgreSQL, selected
  by config ([§8](#8-cloud-strategy)).
- **Central Package Management** (`Directory.Packages.props`) so versions and the
  licensing posture are governed in one place.
- **`.editorconfig` + analyzers** (nullable enabled, warnings-as-errors in CI,
  Roslyn analyzers, StyleCop or equivalent) shipped as a shared config package.
- **Reference application**: a sample service that exercises every package and
  serves as copy-paste-grade documentation.
- **Docs**: this design doc + per-package READMEs + ADRs (Architecture Decision
  Records) capturing each significant choice.
- **Containerization & k8s**: a standard multi-stage `Dockerfile` (chiseled .NET 10
  base) and **Helm chart per service** — probes, graceful shutdown, resource
  limits, ConfigMap/Secret wiring, and KEDA scaling — shipped with the templates so
  a new service is AKS-ready out of the box. ([§8.5](#85-deployment))
- **CI/CD templates**: build, test (with Testcontainers), pack, publish to internal
  NuGet; container build + push to **ACR**; deploy to AKS (GitOps-friendly — Flux /
  Argo CD).

---

## 12. Adoption path <a id="12-migration-path"></a>

Build the framework in a sequence that proves value early and de-risks the big
bets:

1. **Stand up `AGTec.ServiceDefaults` + `AGTec.Security` first** — the most reusable,
   highest-value, lowest-risk assets, built fresh on .NET 10.
2. **Introduce Wolverine (mediator + outbox)** in a thin vertical slice; prove the
   outbox fixes the lost-event gap, with Azure Service Bus in the cloud and RabbitMQ
   locally, selected by config.
3. **Build the reference app** (`AGTec.Reference`) feature by feature on the new
   packages, with an Aspire AppHost for local orchestration.
4. **Standardize the toolkit:** Wolverine for the mediator + bus, Mapperly for
   mapping, FluentValidation for validation, AutoFixture/Bogus for test data,
   `System.Threading.Channels`/the broker for background work — all wired into the
   packages so services don't reinvent them.
5. **Codify learnings into templates** so the *next* service is trivial.
6. For any existing in-house services, migration is opt-in: new work starts on
   AGTecFramework; existing apps adopt packages incrementally with a short per-package
   guide. No big-bang rewrite.

---

## 13. Phased roadmap <a id="13-roadmap"></a>

| Phase | Goal | Key outcomes |
|-------|------|--------------|
| **0 — Decisions** | ✅ Done | Foundational decisions locked ([§14](#14-open-decisions)); layering, stack, cloud posture, and naming agreed |
| **1 — Foundations** | Reusable platform bits | `AGTec.ServiceDefaults`, `AGTec.Security`, `AGTec.Domain`, `AGTec.Application` (Wolverine + behaviors), `AGTec.Abstractions`, CPM + analyzers |
| **2 — Messaging & persistence** | Reliable integration | `AGTec.Messaging` + outbox/inbox/DLQ with Azure Service Bus + RabbitMQ adapters, `AGTec.Persistence.EfCore`, read-model projector, idempotent consumers |
| **3 — Reference app** | Prove it end-to-end | `AGTec.Reference` sample on AGTec, Aspire AppHost, gRPC internal-call sample, Testcontainers integration tests, config-driven provider swap demo |
| **4 — Edge & comms** | Standardize interfaces | `AGTec.Api` (Minimal APIs, Problem Details, OpenAPI/Scalar), `AGTec.Grpc`, YARP gateway conventions |
| **5 — DX & templates** | Make it the default | `dotnet new` templates, CI/CD templates, ADRs, docs, internal NuGet feed |
| **6 — Hardening** | Production readiness | Chaos/load tests, saga sample, **AKS deployment: chiseled images, Helm charts, KEDA autoscaling, Key Vault CSI**, rate limiting, distributed cache (Azure Redis) |

Phases 1–3 are the critical path to proving the thesis; 4–6 make it adoptable.

---

## 14. Decision log <a id="14-open-decisions"></a>

All foundational decisions are **resolved**. This log records them and the rationale
so the roadmap can proceed; changes go through an ADR.

| # | Decision | Resolution | Section |
|---|----------|------------|---------|
| 1 | In-process CQRS + messaging stack | **Wolverine** (MIT) — mediator + middleware + outbox + transport in one | [§6.1](#61-cqrs), [§6.2](#62-messaging) |
| 2 | Primary broker | **Azure Service Bus** default; RabbitMQ local; Kafka optional; config-selected | [§6.2](#62-messaging), [§8.3](#83-matrix) |
| 3 | Cloud posture | **Azure-first, not Azure-only** — ports + config-selected adapters | [§8](#8-cloud-strategy) |
| 4 | Portability depth | **Level 1** — config change + restart, no recompile; Level 2 (hot-swap) out of scope | [§8.2](#82-runtime) |
| 5 | Deployment | **OCI containers on Kubernetes (AKS default)**, portable to any CNCF cluster | [§8.5](#85-deployment) |
| 6 | Write + document + event store | **Marten on PostgreSQL** (one engine; optional event sourcing); MongoDB non-default alternative | [§6.4](#64-persistence) |
| 7 | Search read model | **Elasticsearch / OpenSearch** (portable); Azure AI Search as an alternative adapter | [§6.4](#64-persistence) |
| 8 | HTTP edge style | **Minimal APIs** as the single standard; REST at the edge only | [§6.7](#67-api-edge) |
| 9 | Sync service-to-service | **gRPC** (contract-first); async events the default | [§7](#7-service-to-service-communication) |
| 10 | Object mapping | **Mapperly** (source-gen, MIT) | [§6.5](#65-validation--mapping) |
| 11 | Test stack | **xUnit + AwesomeAssertions** (MIT) + AutoFixture/Bogus + Testcontainers | [§6.8](#68-testing) |
| 12 | Licensing posture | **Permissive OSS only** — exclude MediatR 13+, AutoMapper 15+, MassTransit v9+, FluentAssertions 8+ | [§5.1](#51-the-2025-licensing-shift-why-this-is-urgent-now) |
| 13 | v1 scope | **Packages + reference app + AKS-ready templates** | [§13](#13-roadmap) |
| 14 | Naming | **AGTecFramework** | — |

**Deferred (revisit when the need is concrete — not blocking v1):**

- **Service mesh** (Istio / Linkerd for mTLS & traffic policy) vs. keeping it
  optional at the ingress/gateway layer. Default for now: **mesh optional.**
- **Event sourcing per bounded context** — enabled case by case in Marten, not a
  global default.
- **GitOps umbrella/library Helm charts & centralized secrets platform** — beyond
  the per-service chart that ships in v1.
- **Internal NuGet feed + governance/ADR ownership** — an org/process confirmation,
  not an architectural decision.

---

## 15. Appendix <a id="15-appendix"></a>

### 15.1 Books → practice

| Source | What we take from it |
|--------|----------------------|
| Evans, *Domain-Driven Design* | Bounded contexts, aggregates, ubiquitous language, rich models over anemic ones |
| Vernon, *Implementing Domain-Driven Design* | Aggregate design rules, domain events, application services vs domain services |
| Newman, *Building Microservices* (2nd ed.) | Service boundaries = context boundaries, async-first integration, avoid distributed monolith, gRPC vs REST, sagas |
| Kleppmann, *Designing Data-Intensive Applications* | Dual-write hazards, outbox/CDC, at-least-once + idempotency, eventual consistency, schema evolution, replayable logs |
| Fowler / Richardson (*Microservices Patterns*) | Outbox, saga, API gateway, CQRS, database-per-service patterns |

### 15.2 Key references (verify licensing before adopting)

- .NET 10 (LTS) announcement — https://devblogs.microsoft.com/dotnet/announcing-dotnet-10/
- MediatR / AutoMapper commercial editions — https://www.jimmybogard.com/automapper-and-mediatr-commercial-editions-launch-today/
- MassTransit v9 announcement — https://masstransit.io/introduction/v9-announcement
- Wolverine (JasperFx, MIT) — https://wolverinefx.net/ · Marten — https://martendb.io/
- Mapperly (source-gen mapping) — https://mapperly.riok.app/
- Wolverine Azure Service Bus transport — https://wolverinefx.net/guide/messaging/transports/azureservicebus/
- "MediatR and MassTransit Going Commercial" (analysis) — https://www.milanjovanovic.tech/blog/mediatr-and-masstransit-going-commercial-what-this-means-for-you

---

*This is a living document. Proposed changes go through an ADR + PR against
`docs/`.*
