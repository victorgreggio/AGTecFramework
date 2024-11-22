using System;

namespace AGTec.Common.Randomizer.Impl;

public abstract class RandomGeneratorBase
{
    protected Random randomizer;

    protected RandomGeneratorBase()
    {
        randomizer = new Random((int)DateTime.Now.Ticks);
    }

    protected RandomGeneratorBase(int seed)
    {
        randomizer = new Random(seed);
    }

    protected virtual bool IsConditionToReachLimit()
    {
        return DateTime.Now.Ticks % 2016 == 0;
    }
}