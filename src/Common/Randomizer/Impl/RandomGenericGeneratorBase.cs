using System.Collections.Generic;
using System.Linq;
using AGTec.Common.Randomizer.Validation;

namespace AGTec.Common.Randomizer.Impl;

public abstract class RandomGenericGeneratorBase<T> : RandomGeneratorBase

{
    protected RandomGenericGeneratorBase()
    {
    }

    protected RandomGenericGeneratorBase(int seed)
        : base(seed)

    {
    }

    public virtual T GenerateValueWithin(params T[] values)
    {
        Validator.ValidateNull(values);
        Validator.ValidateCondition(values, item => values.Length > 0);
        Validator.ValidateCondition(values, item => values.Length > 1);

        var numberOfItemsToRandom = values.Length;

        var randomIndex = randomizer.Next(0, numberOfItemsToRandom - 1);

        return values[randomIndex];
    }

    public virtual T GenerateValueApartFrom(params T[] excludedValues)
    {
        T randomValue;
        do
        {
            randomValue = GetRandomValue();
        } while (excludedValues.All(item => EqualityComparer<T>.Default.Equals(item, randomValue)));

        return randomValue;
    }

    protected abstract T GetRandomValue();
}