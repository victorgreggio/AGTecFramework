using System.Collections.Concurrent;
using Microsoft.Extensions.Configuration;

namespace AGTec.Platform;

/// <summary>
/// Default secret store backed by <see cref="IConfiguration"/> (env vars, user secrets,
/// config files). Looks up <c>Secrets:{name}</c>, then <c>{name}</c>. The zero-dependency
/// dev/local default behind <see cref="ISecretStore"/>.
/// </summary>
public sealed class ConfigurationSecretStore(IConfiguration configuration) : ISecretStore
{
    public Task<string?> GetSecretAsync(string name, CancellationToken cancellationToken = default) =>
        Task.FromResult(configuration[$"Secrets:{name}"] ?? configuration[name]);
}

/// <summary>In-memory object store for single-process dev/test. Not durable, not shared.</summary>
public sealed class InMemoryObjectStore : IObjectStore
{
    private readonly ConcurrentDictionary<(string Container, string Key), byte[]> _objects = new();

    public async Task PutAsync(
        string container,
        string key,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default
    )
    {
        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        _objects[(container, key)] = buffer.ToArray();
    }

    public Task<Stream?> GetAsync(string container, string key, CancellationToken cancellationToken = default) =>
        Task.FromResult<Stream?>(
            _objects.TryGetValue((container, key), out var bytes) ? new MemoryStream(bytes) : null
        );

    public Task<bool> ExistsAsync(string container, string key, CancellationToken cancellationToken = default) =>
        Task.FromResult(_objects.ContainsKey((container, key)));

    public Task DeleteAsync(string container, string key, CancellationToken cancellationToken = default)
    {
        _objects.TryRemove((container, key), out _);
        return Task.CompletedTask;
    }
}
