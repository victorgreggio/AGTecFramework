using Marten;
using AGTec.Reference.Application;
using AGTec.Reference.Domain;

namespace AGTec.Reference.Infrastructure;

/// <summary>
/// Write adapter. Uses the Wolverine-managed scoped <see cref="IDocumentSession"/>, so the
/// staged write commits in the same transaction Wolverine uses to flush the outbox.
/// It never calls <c>SaveChangesAsync</c> — the transaction middleware owns the commit.
/// </summary>
public sealed class MartenForecastRepository(IDocumentSession session) : IForecastRepository
{
    public void Add(Forecast forecast)
    {
        var record = new ForecastRecord
        {
            Id = forecast.Id,
            Date = forecast.Date,
            Celsius = forecast.Temperature.Celsius,
            Summary = forecast.Summary,
        };

        session.Store(record);
    }
}

/// <summary>Read adapter. Projects the stored write record into the read DTO.</summary>
public sealed class MartenForecastReadStore(IQuerySession session) : IForecastReadStore
{
    public async Task<ForecastDto?> FindAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var record = await session.LoadAsync<ForecastRecord>(id, cancellationToken);

        return record is null
            ? null
            : new ForecastDto(
                record.Id,
                record.Date,
                record.Celsius,
                new Temperature(record.Celsius).Fahrenheit,
                record.Summary
            );
    }
}
