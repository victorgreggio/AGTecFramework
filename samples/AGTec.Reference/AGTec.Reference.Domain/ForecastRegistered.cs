using AGTec.Domain;

namespace AGTec.Reference.Domain;

/// <summary>
/// Raised in-process when a <see cref="Forecast"/> is first registered. An integration
/// event (e.g. <c>ForecastRegistered v1</c> on the bus, via the outbox) would be derived
/// from this when the fact needs to leave the service.
/// </summary>
public sealed record ForecastRegistered(Guid ForecastId, DateOnly Date, int Celsius) : IDomainEvent
{
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
}
