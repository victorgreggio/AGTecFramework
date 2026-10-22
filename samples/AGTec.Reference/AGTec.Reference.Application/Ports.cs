using AGTec.Reference.Domain;

namespace AGTec.Reference.Application;

/// <summary>
/// Write port for the <see cref="Forecast"/> aggregate. Implemented by a Marten adapter.
/// It only stages the write; the Wolverine/Marten transaction middleware commits it
/// together with the outbox.
/// </summary>
public interface IForecastRepository
{
    void Add(Forecast forecast);
}

/// <summary>
/// Read port for the forecast read model. Keeps the query handler free of Marten so the
/// store can change behind the port.
/// </summary>
public interface IForecastReadStore
{
    Task<ForecastDto?> FindAsync(Guid id, CancellationToken cancellationToken = default);
}
