# AGTec.Messaging

The `IIntegrationEvent` contract: a dependency-free marker for messages that cross a service
boundary. Integration events are versioned, published facts other bounded contexts subscribe to —
they travel via the broker and the transactional outbox, never as a leaked CLR type. Keep them
stable and additive.

Part of [AGTecFramework](https://www.nuget.org/packages?q=AGTecFramework).

## Install

```bash
dotnet add package AGTec.Messaging
```

## What it provides

- `IIntegrationEvent` — marks an integration event and requires two members:
  - `Guid MessageId` — a stable unique id used for idempotent (de-duplicated) consumption.
  - `DateTimeOffset OccurredOn` — when the originating fact occurred (UTC).

## Usage

```csharp
using AGTec.Messaging;

// Carry everything a consumer needs so it never calls back into the producer's database.
public sealed record ForecastRegisteredIntegrationEvent(
    Guid MessageId,
    DateTimeOffset OccurredOn,
    Guid ForecastId,
    DateOnly Date,
    int Celsius,
    string Summary
) : IIntegrationEvent;
```

```csharp
// A command handler returns the event; Wolverine cascades it through the outbox, so it commits
// in the same transaction as the write and can never be lost.
public static (Guid, ForecastRegisteredIntegrationEvent) Handle(RegisterForecast command, IForecastRepository repo)
{
    var forecast = Forecast.Register(command.Date, command.Celsius, command.Summary);
    repo.Add(forecast);

    var @event = new ForecastRegisteredIntegrationEvent(
        MessageId: Guid.CreateVersion7(),
        OccurredOn: DateTimeOffset.UtcNow,
        ForecastId: forecast.Id,
        Date: forecast.Date,
        Celsius: command.Celsius,
        Summary: forecast.Summary);

    return (forecast.Id, @event);
}
```

A consumer handles the event by its type and uses `MessageId` to stay idempotent on at-least-once
redelivery.

## See also

`AGTec.Domain` (`IDomainEvent` — the in-process counterpart that stays inside the bounded context),
`AGTec.Application` (CQRS handlers that cascade these events through the outbox).
