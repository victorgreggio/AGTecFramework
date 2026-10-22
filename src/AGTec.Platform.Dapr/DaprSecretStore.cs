using Dapr.Client;
using AGTec.Platform;

namespace AGTec.Platform.Dapr;

/// <summary>
/// <see cref="ISecretStore"/> backed by the Dapr secrets building block. The actual provider
/// (Key Vault, Vault, local file, …) is chosen in the Dapr component YAML, not in code — so
/// Dapr plugs in behind the same port as the direct adapters, selected by configuration.
/// </summary>
public sealed class DaprSecretStore(DaprClient client, string storeName) : ISecretStore
{
    public async Task<string?> GetSecretAsync(string name, CancellationToken cancellationToken = default)
    {
        try
        {
            var secret = await client.GetSecretAsync(storeName, name, cancellationToken: cancellationToken);
            // Dapr returns a dictionary; a single-value secret is keyed on the secret name.
            return secret.TryGetValue(name, out var value) ? value : secret.Values.FirstOrDefault();
        }
        catch (global::Dapr.DaprException)
        {
            return null;
        }
    }
}
