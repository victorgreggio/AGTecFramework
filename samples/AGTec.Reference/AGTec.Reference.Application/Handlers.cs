namespace AGTec.Reference.Application;

/// <summary>Read handler. Pure projection lookup through the read port.</summary>
public static class GetForecastHandler
{
    public static Task<ForecastDto?> Handle(
        GetForecast query,
        IForecastReadStore readStore,
        CancellationToken cancellationToken
    ) => readStore.FindAsync(query.Id, cancellationToken);
}
