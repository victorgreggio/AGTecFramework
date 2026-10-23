namespace AGTec.Reference.Infrastructure;

/// <summary>
/// Read model owned by the projector (Worker). Built asynchronously from the
/// <c>ForecastRegistered</c> integration event — a separate model from the write store,
/// the essence of CQRS.
/// </summary>
public sealed class ForecastSummary
{
    public Guid Id { get; set; }
    public DateOnly Date { get; set; }
    public int Celsius { get; set; }
    public int Fahrenheit { get; set; }
    public string Summary { get; set; } = string.Empty;
}
