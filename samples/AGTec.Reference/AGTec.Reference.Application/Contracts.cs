using AGTec.Application;
using AGTec.Messaging;

namespace AGTec.Reference.Application;

// ---- Edge DTOs -------------------------------------------------------------

/// <summary>Input bound at the API edge.</summary>
public sealed record RegisterForecastRequest(DateOnly Date, int Celsius, string Summary);

/// <summary>Read-model projection returned to callers.</summary>
public sealed record ForecastDto(Guid Id, DateOnly Date, int Celsius, int Fahrenheit, string Summary);

// ---- Messages (CQRS) -------------------------------------------------------

/// <summary>Write: register a new forecast. Returns the new aggregate id.</summary>
public sealed record RegisterForecast(DateOnly Date, int Celsius, string Summary) : ICommand<Guid>;

/// <summary>Read: fetch a forecast by id.</summary>
public sealed record GetForecast(Guid Id) : IQuery<ForecastDto?>;

// ---- Integration event -----------------------------------------------------

/// <summary>
/// Published (via the transactional outbox) when a forecast is registered, so other
/// services — here, the read-model projector — can react. Carries everything a consumer
/// needs, so it never calls back into this service's database.
/// </summary>
public sealed record ForecastRegisteredIntegrationEvent(
    Guid MessageId,
    DateTimeOffset OccurredOn,
    Guid ForecastId,
    DateOnly Date,
    int Celsius,
    string Summary
) : IIntegrationEvent;
