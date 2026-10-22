namespace AGTec.Reference.Domain;

/// <summary>
/// Value object for a temperature reading. Structural equality and immutability come
/// free from <c>readonly record struct</c>; the only job here is to guard the invariant.
/// </summary>
public readonly record struct Temperature
{
    public const int MinCelsius = -89; // lowest reliably recorded on Earth
    public const int MaxCelsius = 57; // highest reliably recorded on Earth

    public Temperature(int celsius)
    {
        if (celsius is < MinCelsius or > MaxCelsius)
        {
            throw new ArgumentOutOfRangeException(
                nameof(celsius),
                celsius,
                $"Temperature must be between {MinCelsius}°C and {MaxCelsius}°C."
            );
        }

        Celsius = celsius;
    }

    public int Celsius { get; }

    public int Fahrenheit => 32 + (int)(Celsius / 0.5556);
}
