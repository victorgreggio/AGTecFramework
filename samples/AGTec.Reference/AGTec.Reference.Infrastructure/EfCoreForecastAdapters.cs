using AGTec.Reference.Application;
using AGTec.Reference.Domain;
using Microsoft.EntityFrameworkCore;

namespace AGTec.Reference.Infrastructure;

/// <summary>
/// Relational write adapter. Wolverine owns the DbContext transaction and commits it
/// together with its PostgreSQL outbox, so this adapter only stages the aggregate.
/// </summary>
public sealed class EfCoreForecastRepository(ForecastDbContext context) : IForecastRepository
{
    public void Add(Forecast forecast) => context.Forecasts.Add(forecast);
}

/// <summary>Read adapter over the relational aggregate table.</summary>
public sealed class EfCoreForecastReadStore(ForecastDbContext context) : IForecastReadStore
{
    public async Task<ForecastDto?> FindAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context
            .Forecasts.AsNoTracking()
            .Where(forecast => forecast.Id == id)
            .Select(forecast => new ForecastDto(
                forecast.Id,
                forecast.Date,
                forecast.Temperature.Celsius,
                forecast.Temperature.Fahrenheit,
                forecast.Summary
            ))
            .SingleOrDefaultAsync(cancellationToken);
    }
}
