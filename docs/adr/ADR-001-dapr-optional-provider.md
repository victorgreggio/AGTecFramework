# ADR-001: Dapr as an optional provider, not a baseline dependency

- **Status:** Accepted
- **Date:** 2026-10-15
- **Deciders:** Victor Greggio (with Claude Code)

## Context

[Dapr](https://dapr.io/) (Distributed Application Runtime) provides portable
building blocks for microservices — pub/sub, secrets, state, service invocation,
bindings, actors, and workflows — through a **sidecar** the app calls over
HTTP/gRPC. The provider behind each block is chosen in Dapr *component YAML*, not in
code.

Dapr's building blocks overlap substantially with what AGTecFramework already
delivers at the **library** layer (§8 of the design):

| Dapr building block | AGTecFramework today |
|---|---|
| Pub/sub | Wolverine transports, config-selected (Local / RabbitMQ / Azure Service Bus) |
| Secrets | `ISecretStore` + Key Vault / Configuration adapters |
| State management | EF Core/PostgreSQL for relational data; Marten for document/event-sourced models; `IObjectStore` |
| Service invocation (mTLS, retry, discovery) | gRPC + service discovery + Polly resilience |
| Bindings / locks / **actors** / **workflows** | — (genuinely new capabilities) |

So Dapr and our design are two solutions to the same portability goal at different
layers: ours is a **library** abstraction (in-process, typed, testable); Dapr is a
**sidecar** abstraction (language-agnostic, YAML-configured).

## Decision

**Adopt Dapr as an *optional provider behind the existing AGTec ports*, defaulted
off. Do not make it a baseline dependency, and do not replace Wolverine's
EF Core/PostgreSQL transactional outbox with Dapr pub/sub.**

Because cloud-coupled resources already sit behind ports (`ISecretStore`,
`IObjectStore`, config-selected messaging), Dapr becomes *just another adapter
choice*, selected by configuration exactly like the Azure adapters:

```
AGTec:Secrets:Provider = Configuration | AzureKeyVault | Dapr
```

## Spike (concrete, runnable)

A spike proved the pattern end to end:

- `src/AGTec.Platform.Dapr` — `DaprSecretStore : ISecretStore` over `Dapr.Client`.
- Wired into the reference service's config-selected secrets provider.
- `tests/AGTec.Platform.Tests/DaprSecretStoreTests` — runs a **real `daprd` sidecar**
  (Testcontainers) with a local-file secret store component and reads a secret
  through the adapter (and returns null for a missing one). **Passing.**
- `AGTec.Reference.AppHost` — `.WithDaprSidecar()` shows Aspire injecting the sidecar
  for local dev (requires the Dapr CLI to run).

Findings: the adapter is ~20 lines and slots in with zero change above the port;
the Dapr backend (Key Vault, Vault, local file, …) is pure component YAML. The cost
is operational (a sidecar per pod + the Dapr control plane on AKS), not code.

## Consequences

**Positive**
- Portability (swap a backend via component YAML, no recompile) — delivered by the
  platform for any block Dapr covers.
- **Polyglot**: the same building blocks for non-.NET services (e.g. the Python
  Strategy product) — the strongest reason to adopt.
- Access to capabilities we don't build ourselves: **durable workflows**, **actors**,
  distributed lock, mTLS service invocation.
- Our ports design is validated — Dapr is a drop-in, not a rewrite.

**Negative / cautions**
- A **sidecar per pod** plus the Dapr control plane to operate, secure, and upgrade
  on AKS; an extra localhost hop.
- Lowest-common-denominator APIs (CloudEvents envelope, generic state) vs typed
  Wolverine messages.
- **Dapr pub/sub does not match the Wolverine + EF Core/PostgreSQL transactional outbox** we
  rely on, so messaging stays on Wolverine; Dapr pub/sub is not adopted.

## When to adopt Dapr (beyond the optional secret/state providers)

Adopt a block if **any** of these is true, otherwise prefer the direct adapters:

1. **Polyglot is real** — other languages need the same building blocks.
2. You need **durable workflows or the actor model** (complements, not replaces,
   Wolverine).
3. You want **mTLS service-to-service** without running a full service mesh.

## Alternatives considered

- **Dapr-first** (sidecar for messaging/state/secrets/invocation): rejected for now —
  gives up the Wolverine EF Core/PostgreSQL outbox tightness and adds control-plane ops before a
  concrete polyglot requirement justifies it.
- **No Dapr**: viable; the direct adapters already satisfy the .NET-only case. This
  ADR keeps that as the default while leaving the door open at near-zero cost.
