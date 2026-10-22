namespace AGTec.Platform;

/// <summary>
/// Filesystem-backed <see cref="IObjectStore"/> — the local default for services that need
/// durable files without a cloud account. Lays objects out as <c>{root}/{container}/{key}</c>
/// and guards against keys escaping the root. Content type is not persisted (the filesystem has
/// no notion of it); use the Azure Blob adapter where that matters.
/// </summary>
public sealed class FileSystemObjectStore : IObjectStore
{
    private readonly string _root;

    public FileSystemObjectStore(string root)
    {
        _root = Path.GetFullPath(root);
        Directory.CreateDirectory(_root);
    }

    public async Task PutAsync(
        string container,
        string key,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default
    )
    {
        var path = ResolvePath(container, key);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await using var file = File.Create(path);
        await content.CopyToAsync(file, cancellationToken);
    }

    public Task<Stream?> GetAsync(string container, string key, CancellationToken cancellationToken = default)
    {
        var path = ResolvePath(container, key);
        return Task.FromResult<Stream?>(File.Exists(path) ? File.OpenRead(path) : null);
    }

    public Task<bool> ExistsAsync(string container, string key, CancellationToken cancellationToken = default) =>
        Task.FromResult(File.Exists(ResolvePath(container, key)));

    public Task DeleteAsync(string container, string key, CancellationToken cancellationToken = default)
    {
        var path = ResolvePath(container, key);
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        return Task.CompletedTask;
    }

    private string ResolvePath(string container, string key)
    {
        var combined = Path.GetFullPath(Path.Combine(_root, container, key));
        var prefix = _root + Path.DirectorySeparatorChar;

        // Reject containers/keys that traverse outside the store root (e.g. "../").
        if (combined != _root && !combined.StartsWith(prefix, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Resolved path escapes the object store root: '{container}/{key}'.");
        }

        return combined;
    }
}
