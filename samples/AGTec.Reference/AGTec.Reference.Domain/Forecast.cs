using AGTec.Domain;

namespace AGTec.Reference.Domain;

/// <summary>
/// The <c>Forecast</c> aggregate root. All mutation goes through intention-revealing
/// methods that keep the aggregate valid; there are no public setters. Compare with the
/// anemic "data bag + logic in handlers" style the framework deliberately moves away from.
/// </summary>
public sealed class Forecast : AggregateRoot<Guid>
{
    // Private: EF Core / Marten materialize through this; callers use Register().
    private Forecast(Guid id, DateOnly date, Temperature temperature, string summary)
        : base(id)
    {
        Date = date;
        Temperature = temperature;
        Summary = summary;
    }

    public DateOnly Date { get; private set; }

    public Temperature Temperature { get; private set; }

    public string Summary { get; private set; }

    /// <summary>Factory: the only way to bring a new forecast into existence.</summary>
    public static Forecast Register(DateOnly date, int celsius, string summary)
    {
        if (string.IsNullOrWhiteSpace(summary))
        {
            throw new ArgumentException("Summary is required.", nameof(summary));
        }

        var forecast = new Forecast(Guid.CreateVersion7(), date, new Temperature(celsius), summary.Trim());
        forecast.Raise(new ForecastRegistered(forecast.Id, date, celsius));
        return forecast;
    }

    /// <summary>Behavior lives on the aggregate, not in a handler.</summary>
    public void ReviseSummary(string summary)
    {
        if (string.IsNullOrWhiteSpace(summary))
        {
            throw new ArgumentException("Summary is required.", nameof(summary));
        }

        Summary = summary.Trim();
    }
}
