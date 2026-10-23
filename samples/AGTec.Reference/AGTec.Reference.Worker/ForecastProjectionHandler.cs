using AGTec.Reference.Application;
using AGTec.Reference.Domain;
using AGTec.Reference.Infrastructure;
using Marten;

namespace AGTec.Reference.Worker;

/// <summary>
/// The read-model projector. Consumes the <see cref="ForecastRegisteredIntegrationEvent"/> integration
/// event (delivered at-least-once from the outbox) and builds the read models: a Marten
/// document for lookups and a search index entry. It owns its own data and never calls
/// back into the producer's database.
/// </summary>
public static class ForecastProjectionHandler
{
    public static async Task Handle(
        ForecastRegisteredIntegrationEvent @event,
        IDocumentSession session,
        IForecastSearchStore search,
        CancellationToken cancellationToken
    )
    {
        var fahrenheit = new Temperature(@event.Celsius).Fahrenheit;

        // Document read model (upsert keyed by id → idempotent on redelivery).
        session.Store(
            new ForecastSummary
            {
                Id = @event.ForecastId,
                Date = @event.Date,
                Celsius = @event.Celsius,
                Fahrenheit = fahrenheit,
                Summary = @event.Summary,
            }
        );

        // Search read model.
        await search.IndexAsync(
            new ForecastDto(@event.ForecastId, @event.Date, @event.Celsius, fahrenheit, @event.Summary),
            cancellationToken
        );
    }
}
