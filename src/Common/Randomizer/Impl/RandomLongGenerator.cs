using System;
using AGTec.Common.Randomizer.ValueTypes;

namespace AGTec.Common.Randomizer.Impl;

public class RandomLongGenerator : RandomGenericGeneratorBase<long>, IRandomLong
{
    public RandomLongGenerator()
    {
    }

    public RandomLongGenerator(int seed)
        : base(seed)
    {
    }

    public long GenerateValue()
    {
        return GetRandomValue();
    }

    public long GenerateValue(long min, long max)
    {
        if (min >= max) throw new ArgumentException(Constants.MinMaxValueExceptionMsg);

        if (IsConditionToReachLimit()) return max;
        var randomLong = (long)randomizer.NextDouble();
        return min + randomLong * max - randomLong * min;
    }

    public long GeneratePositiveValue()
    {
        if (IsConditionToReachLimit()) return long.MaxValue;

        return (long)(randomizer.NextDouble() * long.MaxValue);
    }

    public long GenerateNegativeValue()
    {
        if (IsConditionToReachLimit()) return long.MinValue;

        return (long)(randomizer.NextDouble() * long.MinValue);
    }

    protected override long GetRandomValue()
    {
        var randomPositive = (long)(randomizer.NextDouble() * long.MaxValue);
        var randomNegative = (long)(randomizer.NextDouble() * long.MinValue);
        if (IsConditionToReachLimit()) return long.MaxValue;

        return randomNegative + randomPositive;
    }
}