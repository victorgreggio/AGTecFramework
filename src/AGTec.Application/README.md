# AGTec.Application

CQRS message contracts (`ICommand`, `ICommand<TResult>`, `IQuery<TResult>`) and AGTecFramework's
standard [Wolverine](https://wolverinefx.net) application pipeline: automatic FluentValidation and
structured, timed request logging on every handled message. Persistence-agnostic — EF
Core/PostgreSQL transactions and the outbox, or a deliberate Marten document/event
store integration, are added by the persistence layer.

Part of [AGTecFramework](https://www.nuget.org/packages?q=AGTecFramework).

## Install

```bash
dotnet add package AGTec.Application
```

## What it provides

- `ICommand` / `ICommand<TResult>` — marker interfaces for state-changing requests, each handled
  by exactly one handler; the generic form returns a result (e.g. a new aggregate id).
- `IQuery<TResult>` — marker interface for read requests that change nothing.
- `AGTecWolverineExtensions.ApplyAGTecDefaults(this WolverineOptions)` — applies the standard
  pipeline: `UseFluentValidation()` plus the request-logging middleware.
- `RequestLoggingMiddleware` — Wolverine middleware that logs and times every handled message
  (source-generated logging, no per-call allocations).

## Usage

```csharp
using AGTec.Application;

// 1. Define messages as records implementing the markers.
public sealed record RegisterForecast(DateOnly Date, int Celsius, string Summary) : ICommand<Guid>;
public sealed record GetForecast(Guid Id) : IQuery<ForecastDto?>;

// 2. Handlers are discovered by Wolverine's type/method convention.
public static class RegisterForecastHandler
{
    public static Guid Handle(RegisterForecast command, IForecastRepository repository)
    {
        var forecast = Forecast.Register(command.Date, command.Celsius, command.Summary);
        repository.Add(forecast);
        return forecast.Id;   // response to InvokeAsync<Guid>
    }
}
```

```csharp
using AGTec.Application;
using Wolverine;

// 3. Wire the AGTec pipeline into the host (persistence/outbox added separately).
builder.UseWolverine(options =>
{
    options.ApplyAGTecDefaults();                                 // validation + request logging
    options.Discovery.IncludeAssembly(typeof(RegisterForecast).Assembly);
});

// 4. Send messages through the bus (e.g. from a Minimal API endpoint).
var id = await bus.InvokeAsync<Guid>(new RegisterForecast(date, celsius, summary), ct);
```

Register a FluentValidation `AbstractValidator<T>` (e.g. `IValidator<RegisterForecast>`) in DI and
the pipeline runs it automatically before the message reaches its handler.

## See also

`AGTec.Domain` (the aggregates these commands mutate), `AGTec.Messaging` (integration events a handler
can cascade through the transactional outbox).
