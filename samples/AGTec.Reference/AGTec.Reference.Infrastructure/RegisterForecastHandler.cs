using AGTec.Reference.Application;
using AGTec.Reference.Domain;

namespace AGTec.Reference.Infrastructure;

/// <summary>
/// Persists the aggregate and returns its integration event for Wolverine's transactional outbox.
/// </summary>
public static class RegisterForecastHandler
{
    public static (Guid, ForecastRegisteredIntegrationEvent) Handle(RegisterForecast command, ForecastDbContext context)
    {
        var forecast = Forecast.Register(command.Date, command.Celsius, command.Summary);
        context.Forecasts.Add(forecast);

        var @event = new ForecastRegisteredIntegrationEvent(
            MessageId: Guid.CreateVersion7(),
            OccurredOn: DateTimeOffset.UtcNow,
            ForecastId: forecast.Id,
            Date: forecast.Date,
            Celsius: forecast.Temperature.Celsius,
            Summary: forecast.Summary
        );

        return (forecast.Id, @event);
    }
}
