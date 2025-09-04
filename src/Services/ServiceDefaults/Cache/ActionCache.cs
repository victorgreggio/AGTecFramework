using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace AGTec.Services.ServiceDefaults.Cache;

public static class ActionCache
{
    private static readonly IMemoryCache MemoryCache =
        new MemoryCache(new OptionsWrapper<MemoryCacheOptions>(new MemoryCacheOptions
        {
            ExpirationScanFrequency = TimeSpan.FromSeconds(5)
        }));

    public static T Cache<T>(Expression<Func<T>> action, [CallerMemberName] string memberName = "")
    {
        var body = (MethodCallExpression)action.Body;

        ICollection<object> parameters = new List<object>();

        foreach (var argument in body.Arguments)
            if (argument is MemberExpression expression)
                parameters.Add(
                    ((FieldInfo)expression.Member).GetValue(((ConstantExpression)expression.Expression).Value));

        var builder = new StringBuilder();
        builder.Append(action.GetType().FullName);
        builder.Append(".");
        builder.Append(memberName);

        parameters.ToList().ForEach(x =>
        {
            builder.Append("_");
            builder.Append(x);
        });

        var cacheKey = builder.ToString();

        var retrieve = MemoryCache.Get<T>(cacheKey);

        if (retrieve == null)
        {
            retrieve = action.Compile().Invoke();
            MemoryCache.Set(cacheKey, retrieve,
                new MemoryCacheEntryOptions { SlidingExpiration = TimeSpan.FromSeconds(2) }
            );
        }

        return retrieve;
    }
}
