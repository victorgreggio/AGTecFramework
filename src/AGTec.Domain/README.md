# AGTec.Domain

Dependency-free domain primitives for domain-driven design: identity-based `Entity<TId>`, the
`AggregateRoot<TId>` consistency boundary with built-in domain-event recording, and the
`IDomainEvent` marker. These are the building blocks of a rich, behavior-first domain model.

Part of [AGTecFramework](https://www.nuget.org/packages?q=AGTecFramework).

## Install

```bash
dotnet add package AGTec.Domain
```

## What it provides

- `Entity<TId>` — base class for entities whose equality is defined by a stable, immutable `Id`
  (set once at construction), not by their attributes. Implements `IEquatable<>`, `==` and `!=`.
- `AggregateRoot<TId>` — base class for aggregate roots (extends `Entity<TId>`). Exposes
  `DomainEvents`, an optimistic-concurrency `Version`, the protected `Raise(IDomainEvent)` to
  record events, and `ClearDomainEvents()` for the infrastructure to call after dispatch.
- `IDomainEvent` — marks an in-process domain event raised inside the aggregate's transaction;
  carries an `OccurredOn` (UTC) timestamp.

## Usage

```csharp
using AGTec.Domain;

public sealed record ForecastRegistered(Guid ForecastId, int Celsius) : IDomainEvent
{
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
}

public sealed class Forecast : AggregateRoot<Guid>
{
    private Forecast(Guid id, string summary) : base(id) => Summary = summary;

    public string Summary { get; private set; }

    // Factory is the only way to create one — invariants enforced here, no public setters.
    public static Forecast Register(int celsius, string summary)
    {
        if (string.IsNullOrWhiteSpace(summary))
            throw new ArgumentException("Summary is required.", nameof(summary));

        var forecast = new Forecast(Guid.CreateVersion7(), summary.Trim());
        forecast.Raise(new ForecastRegistered(forecast.Id, celsius));  // dispatched on commit
        return forecast;
    }
}
```

The persistence layer maps `Version` to a row version for optimistic concurrency, publishes each
item in `DomainEvents` when the unit of work commits, then calls `ClearDomainEvents()`.

## See also

`AGTec.Application` (CQRS contracts + Wolverine pipeline that handle the commands mutating these
aggregates), `AGTec.Messaging` (integration events for facts that must leave the bounded context).
