using System;
using AGTec.Common.Randomizer.ValueTypes;

namespace AGTec.Common.Randomizer.Impl;

public sealed class RandomFloatGenerator : RandomGenericGeneratorBase<float>, IRandomFloat
{
    public RandomFloatGenerator()
    {
    }

    public RandomFloatGenerator(int seed)
        : base(seed)
    {
    }

    public float GenerateValue()
    {
        var randomPositive = (float)randomizer.NextDouble() * float.MinValue;
        var randomNegative = (float)randomizer.NextDouble() * float.MaxValue;

        return randomPositive + randomNegative;
    }

    public float GenerateValue(float min, float max)
    {
        if (min >= max) throw new ArgumentException(Constants.MinMaxValueExceptionMsg);

        if (IsConditionToReachLimit()) return max;

        var randomFloat = (float)randomizer.NextDouble();
        return min + randomFloat * max - randomFloat * min;
    }

    public float GeneratePositiveValue()
    {
        if (IsConditionToReachLimit()) return float.MaxValue;

        return (float)randomizer.NextDouble() * float.MaxValue;
    }

    public float GenerateNegativeValue()
    {
        if (IsConditionToReachLimit()) return float.MinValue;

        return (float)randomizer.NextDouble() * float.MinValue;
    }

    protected override float GetRandomValue()
    {
        var randomPositive = (float)randomizer.NextDouble() * float.MinValue;
        var randomNegative = (float)randomizer.NextDouble() * float.MaxValue;

        return randomPositive + randomNegative;
    }
}