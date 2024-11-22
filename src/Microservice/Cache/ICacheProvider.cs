using System;

namespace AGTec.Microservice.Cache;

public interface ICacheProvider
{
    T Get<T>(string key);
    void Remove(string key);
    void Set(string key, object value, TimeSpan expirationTimeSpan);
}