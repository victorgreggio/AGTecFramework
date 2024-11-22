namespace AGTec.Common.Randomizer;

public interface IRandomDigit<T>
    : IRandomValueType<T>
    where T : struct
{
    T GeneratePositiveValue();

    T GenerateNegativeValue();
}