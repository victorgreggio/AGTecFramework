namespace AGTec.Common.Randomizer;

public interface IRandomValueType<T> : IRandom
    where T : struct
{
    T GenerateValue();

    T GenerateValue(T min, T max);

    T GenerateValueWithin(params T[] values);

    T GenerateValueApartFrom(params T[] values);
}