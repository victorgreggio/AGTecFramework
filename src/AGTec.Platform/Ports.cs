namespace AGTec.Platform;

/// <summary>
/// Provider-neutral secret store. Azure Key Vault in production (or any provider),
/// configuration/env for dev — selected by configuration, swapped without recompiling.
/// </summary>
public interface ISecretStore
{
    Task<string?> GetSecretAsync(string name, CancellationToken cancellationToken = default);
}

/// <summary>
/// Provider-neutral object (blob) store. Azure Blob Storage in production (or S3, …),
/// in-memory for dev/test. A stream-based port so no cloud SDK leaks above infrastructure.
/// </summary>
public interface IObjectStore
{
    Task PutAsync(
        string container,
        string key,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default
    );

    Task<Stream?> GetAsync(string container, string key, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(string container, string key, CancellationToken cancellationToken = default);

    Task DeleteAsync(string container, string key, CancellationToken cancellationToken = default);
}
