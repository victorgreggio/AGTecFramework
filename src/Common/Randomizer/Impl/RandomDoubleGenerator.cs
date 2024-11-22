using System;
using AGTec.Common.Randomizer.ValueTypes;

namespace AGTec.Common.Randomizer.Impl;

public class RandomDoubleGenerator : RandomGenericGeneratorBase<double>, IRandomDouble
{
    public RandomDoubleGenerator()
    {
    }

    public RandomDoubleGenerator(int seed)
        : base(seed)
    {
    }

    public double GenerateValue()
    {
        return GetRandomValue();
    }

    public double GenerateValue(double min, double max)
    {
        if (min >= max) throw new ArgumentException(Constants.MinMaxValueExceptionMsg);

        if (IsConditionToReachLimit()) return max;
        var randomDouble = randomizer.NextDouble();
        return min + randomDouble * max - randomDouble * min;
    }

    public double GeneratePositiveValue()
    {
        if (IsConditionToReachLimit()) return double.MaxValue;

        return randomizer.NextDouble() * double.MaxValue;
    }

    public double GenerateNegativeValue()
    {
        if (IsConditionToReachLimit()) return double.MinValue;

        return randomizer.NextDouble() * double.MinValue;
    }

    protected override double GetRandomValue()
    {
        var randomPositive = randomizer.NextDouble() * double.MaxValue;
        var randomNegative = randomizer.NextDouble() * double.MinValue;
        return randomPositive + randomNegative;
    }
}