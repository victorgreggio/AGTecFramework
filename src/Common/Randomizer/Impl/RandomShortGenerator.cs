using System;
using AGTec.Common.Randomizer.ValueTypes;

namespace AGTec.Common.Randomizer.Impl;

public class RandomShortGenerator : RandomGenericGeneratorBase<short>, IRandomShort
{
    public RandomShortGenerator()
    {
    }

    public RandomShortGenerator(int seed)
        : base(seed)
    {
    }

    public short GenerateValue()
    {
        return GetRandomValue();
    }

    public short GenerateValue(short min, short max)
    {
        if (min >= max) throw new ArgumentException(Constants.MinMaxValueExceptionMsg);
        if (IsConditionToReachLimit()) return min;
        return (short)randomizer.Next(min, max);
    }

    public short GeneratePositiveValue()
    {
        if (IsConditionToReachLimit()) return short.MaxValue;

        return (short)randomizer.Next(0, short.MaxValue);
    }

    public short GenerateNegativeValue()
    {
        if (IsConditionToReachLimit()) return 0;

        return (short)randomizer.Next(short.MinValue, 0);
    }

    protected override short GetRandomValue()
    {
        var randomPositive = (short)randomizer.Next(0, short.MaxValue);
        var randomNegative = (short)randomizer.Next(short.MinValue, 0);
        if (IsConditionToReachLimit()) return short.MaxValue;

        return (short)(randomPositive + randomNegative);
    }
}