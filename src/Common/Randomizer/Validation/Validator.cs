using System;

namespace AGTec.Common.Randomizer.Validation;

public static class Validator
{
    public static void ValidateNull(object item)
    {
        if (item == null) throw new ArgumentNullException("item", "Reference is null.");
    }

    public static void ValidateNullOrEmpty(string item)
    {
        if (string.IsNullOrEmpty(item)) throw new ArgumentNullException("item", "String is null or empty");
    }

    public static void ValidateCondition<TElement>(TElement element, Func<TElement, bool> condition)
    {
        if (!condition(element)) throw new Exception("Validation result of condition failed.");
    }
}