using AGTec.Reference.Domain;

namespace AGTec.Reference.Application;

/// <summary>
/// Write handler. Discovered by Wolverine (type/method convention). It creates the
/// aggregate, stages the write through the port, and <b>returns</b> the integration event
/// — Wolverine cascades it through the transactional outbox, so the event is committed in
/// the same transaction as the write and can never be lost.
/// </summary>
public static class RegisterForecastHandler
{
    public static (Guid, ForecastRegisteredIntegrationEvent) Handle(
        RegisterForecast command,
        IForecastRepository repository
    )
    {
        var forecast = Forecast.Register(command.Date, command.Celsius, command.Summary);

        repository.Add(forecast);

        var @event = new ForecastRegisteredIntegrationEvent(
            MessageId: Guid.CreateVersion7(),
            OccurredOn: DateTimeOffset.UtcNow,
            ForecastId: forecast.Id,
            Date: forecast.Date,
            Celsius: forecast.Temperature.Celsius,
            Summary: forecast.Summary
        );

        // Tuple return: the Guid is the response to InvokeAsync<Guid>; the event cascades.
        return (forecast.Id, @event);
    }
}

/// <summary>Read handler. Pure projection lookup through the read port.</summary>
public static class GetForecastHandler
{
    public static Task<ForecastDto?> Handle(
        GetForecast query,
        IForecastReadStore readStore,
        CancellationToken cancellationToken
    ) => readStore.FindAsync(query.Id, cancellationToken);
}
