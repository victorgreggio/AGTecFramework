using System;
using System.Collections.Generic;
using System.Linq;

namespace AGTec.Common.Base.Extensions;

public static class EnumerableExtension
{
    public static bool IsNullOrEmpty<T>(this IEnumerable<T> data)
    {
        return data == null || data.Any() == false;
    }

    public static bool HasOnlyOneElement<T>(this IEnumerable<T> data)
    {
        return data.Skip(1).Any() == false;
    }

    public static void ForEach<T>(this IEnumerable<T> list, Action<T> block)
    {
        foreach (var item in list) block(item);
    }
}