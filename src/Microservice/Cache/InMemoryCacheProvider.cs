﻿using System;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;

namespace AGTec.Microservice.Cache;

public class InMemoryCacheProvider : ICacheProvider
{
    private readonly IMemoryCache _memoryCache;

    public InMemoryCacheProvider(IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache;
    }

    public T Get<T>(string key)
    {
        if (_memoryCache.TryGetValue(key, out var cachedObject))
        {
            var content = cachedObject as string;
            if (string.IsNullOrWhiteSpace(content) == false)
                return JsonConvert.DeserializeObject<T>(content);
        }

        return default;
    }

    public void Remove(string key)
    {
        _memoryCache.Remove(key);
    }

    public void Set(string key, object value, TimeSpan expirationTimeSpan)
    {
        var content = JsonConvert.SerializeObject(value);
        if (string.IsNullOrWhiteSpace(content) == false)
            _memoryCache.Set(key, content,
                new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = expirationTimeSpan });
    }
}