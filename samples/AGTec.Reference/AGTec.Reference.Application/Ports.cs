using AGTec.Reference.Domain;

namespace AGTec.Reference.Application;

/// <summary>
/// Write port for the <see cref="Forecast"/> aggregate. Implemented by an EF Core adapter.
/// It only stages the write; the Wolverine/EF Core transaction middleware commits it
/// together with the outbox.
/// </summary>
public interface IForecastRepository
{
    void Add(Forecast forecast);
}

/// <summary>
/// Read port for the forecast read model. Keeps the query handler free of EF Core so the
/// store can change behind the port.
/// </summary>
public interface IForecastReadStore
{
    Task<ForecastDto?> FindAsync(Guid id, CancellationToken cancellationToken = default);
}
