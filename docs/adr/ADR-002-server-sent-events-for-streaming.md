# ADR-002: Server-Sent Events (SSE) for server→client streaming, not WebSockets

- **Status:** Accepted
- **Date:** 2026-10-20
- **Deciders:** Victor Greggio (with Claude Code)

## Context

AGTec applications routinely need to push updates from the server to the browser:

- progress of a long-running command or agent/LLM call (tokens, steps, partial results);
- status of **asynchronous** work processed off the request thread — a Wolverine/Marten
  outbox message handled in-process or by a Worker (e.g. a document ingestion pipeline
  reporting `Pending → Processing → Completed/Failed`);
- any "it will be ready soon" state the UI would otherwise **poll** for.

There are two mainstream browser transports for this:

| | **SSE** (`text/event-stream`) | **WebSocket** |
|---|---|---|
| Direction | **Server → client** (one-way) | Full-duplex |
| Protocol | Plain HTTP response that stays open | Separate `Upgrade` protocol |
| Auth | Normal HTTP (Bearer header over `fetch`) | Per-socket handshake auth |
| Reverse proxy | Ordinary HTTP (disable buffering) | Needs `Upgrade`/`Connection` handling |
| Scale-out | Stateless; any instance | Long-lived affinity / sticky sessions |
| Reconnect | Built into the protocol | Hand-rolled |
| Server code | Write frames to the response body | Connection/session lifecycle to manage |

Crucially, a AGTec app's **client→server** direction is already covered by ordinary HTTP
requests (commands/queries dispatched through Wolverine). So the open need is almost
always **one-way push**, which is exactly SSE's shape — and AGTec already ships a
reverse-proxy template (`templates/agtec-ui`) that is configured for streaming responses.

## Decision

**Use Server-Sent Events for server→client streaming by default. Do not add a WebSocket
(or SignalR) stack to AGTecFramework unless a concrete full-duplex, low-latency requirement
appears; when it does, introduce it deliberately as its own capability rather than as the
baseline for all streaming.**

SSE is the lowest-cost transport that satisfies the common cases: it rides the existing
HTTP edge, authenticates with the same Bearer token, survives the same reverse proxy, and
needs no sticky sessions or connection registry.

### How a AGTec app implements it

**Server — prefer the framework's built-in result.** .NET 10 ships `TypedResults.ServerSentEvents`
(`System.Net.ServerSentEvents.SseItem<T>`), which sets the headers, frames `event:` / `data:`, flushes each
item, serializes the payload as JSON, and honours cancellation. Return it from a minimal-API endpoint over an
`IAsyncEnumerable` — no hand-rolled framing:

```csharp
app.MapGet("/tenants/{tenantId}/.../things-stream",
    (string tenantId, HttpContext http, IMessageBus bus, CancellationToken ct) =>
{
    http.Response.Headers["X-Accel-Buffering"] = "no"; // belt-and-suspenders: never buffer at a proxy
    return TypedResults.ServerSentEvents(Source(bus, tenantId, ct));
}).RequireAuthorization(/* same policy as the rest */);

// Each item is a named SSE frame; SseItem<object> serializes the value by its *runtime* type, which
// preserves a polymorphic event hierarchy (e.g. distinct `question_parsed` vs `parsing_complete` shapes).
static async IAsyncEnumerable<SseItem<object>> Source(
    IMessageBus bus, string tenantId, [EnumeratorCancellation] CancellationToken ct)
{
    await foreach (var evt in Work(ct))
        yield return new SseItem<object>(evt, evt.Name);
}
```

Only reach for manual framing (`Response.WriteAsync($"event: …\ndata: …\n\n")` + `FlushAsync`) on a runtime
without the built-in result.

Two shapes of `Source`:

- **Progress stream** — stream directly from the in-process `IAsyncEnumerable` the work
  produces (agent/LLM steps, parse events). Truly event-driven.
- **Status stream** — for async work handled off-thread (outbox/Worker), **watch
  server-side**: re-read the read model on a short interval and emit a frame only when it
  changes, closing with a terminal `complete` event once everything has settled (and a
  safety deadline). This replaces *client* polling with a single pushed stream and works
  whether the work ran in-process or in a Worker. (Optional refinement: wake the loop with
  an in-process notifier for instant updates; keep the interval as the cross-process fallback.)

Always honor the `CancellationToken` (it fires on client disconnect) and bound the stream
with a deadline so a connection can't live forever.

**Client — read over `fetch`, not `EventSource`:** the browser's `EventSource` cannot set
an `Authorization` header or issue a `POST`, so read the stream with `fetch` +
`ReadableStream` and split frames on the blank line (`\n\n`), dispatching by `event:` name.

**Reverse proxy — use `templates/agtec-ui`:** its `nginx.conf.template` is already
SSE-ready (`proxy_buffering off`, `proxy_cache off`, `proxy_read_timeout 3600s`,
`Connection ''`). Any proxy in front of a AGTec API must disable response buffering on the
stream path, which is why the server also sends `X-Accel-Buffering: no`.

## Consequences

**Positive**
- One transport, no new dependency; streaming reuses the existing HTTP edge, auth, and the
  shipped nginx template.
- Stateless and horizontally scalable — no sticky sessions or connection registry.
- Eliminates client polling (chatty, laggy) with a clean pushed stream that self-closes.
- Trivial, testable server code (write frames); degrades to a plain request on failure.

**Negative / cautions**
- One-way only — not for genuine bidirectional/interactive flows (see below).
- A long-lived HTTP connection per client; bound it with a deadline and honor cancellation.
- Proxies **must** disable buffering on the stream path or frames stall until the buffer fills.
- SSE is UTF-8 text; binary payloads must be encoded (e.g. base64) or fetched separately.

## When to use WebSockets instead

Reach for WebSockets (or SignalR) only when the interaction is genuinely **full-duplex or
low-latency bidirectional**: collaborative editing, presence/typing indicators,
bidirectional RPC, or high-frequency client→server input (e.g. live cursors). Add it then
as its own concern, with explicit decisions about per-socket auth and session affinity —
not as the default for one-way push.

## Alternatives considered

- **Client polling** — rejected: chatty, adds latency, no server-driven completion, and
  scales poorly as an app grows more live surfaces.
- **WebSockets as the baseline** — rejected for now: full-duplex machinery (connection
  lifecycle, per-socket auth, sticky sessions, proxy `Upgrade` config) for a one-way need.
  Kept available for a future, concrete bidirectional requirement.
- **SignalR** — rejected as the default: adds a hub/negotiation dependency and (with
  WebSocket/long-polling fallbacks) sticky-session concerns; heavier than SSE for one-way
  push. Reasonable if a project later needs managed hubs for bidirectional scenarios.
