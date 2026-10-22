# AGTec.Platform

Provider-neutral platform ports — secrets (`ISecretStore`), object storage (`IObjectStore`),
malware scanning (`IMalwareScanner`) and signed URLs (`IObjectUrlProvider`) — with zero-dependency
defaults. Cloud adapters (`AGTec.Platform.Azure`, `AGTec.Platform.ClamAv`, `AGTec.Platform.Dapr`) plug
in behind the same ports, selected by configuration. No cloud SDKs.

Part of [AGTecFramework](https://www.nuget.org/packages?q=AGTecFramework).

## Install

```bash
dotnet add package AGTec.Platform
```

## What it provides

- `ISecretStore` — single-method secret lookup. Default `ConfigurationSecretStore` reads
  `Secrets:{name}` then `{name}` from `IConfiguration` (env vars, user secrets, config files).
- `IObjectStore` — stream-based blob store (`Put`/`Get`/`Exists`/`Delete`). Defaults:
  `InMemoryObjectStore` (dev/test) and `FileSystemObjectStore` (durable, `{root}/{container}/{key}`).
- `IMalwareScanner` + `MalwareScanResult` / `MalwareDetectedException` — default `NoOpMalwareScanner`
  (treats everything as clean). `MalwareScanningObjectStore` decorates an `IObjectStore` so every
  upload is scanned before it is stored.
- `IObjectUrlProvider` — short-lived signed upload/download URLs for direct client transfer (no
  default; implemented by storage adapters such as Azure Blob SAS).
- DI helpers on `AGTecPlatformExtensions`: `AddConfigurationSecrets()`, `AddInMemoryStorage()`,
  `AddFileSystemStorage(root)`, `AddNoOpMalwareScanner()`, `ScanObjectStoreForMalware()`, and a
  generic `Decorate<TService>(...)`.

## Usage

```csharp
using AGTec.Platform;
using Microsoft.Extensions.DependencyInjection;

services.AddConfigurationSecrets();                  // ISecretStore
services.AddFileSystemStorage("local-data/blobs");   // IObjectStore
services.AddNoOpMalwareScanner();                     // IMalwareScanner
services.ScanObjectStoreForMalware();                 // wraps IObjectStore with scanning

// in a handler/service:
public sealed class Uploader(IObjectStore store, ISecretStore secrets)
{
    public async Task SaveAsync(string key, Stream content, CancellationToken ct)
    {
        await store.PutAsync("uploads", key, content, "application/pdf", ct);  // scanned, then stored
    }
}
```

## Configuration

`ConfigurationSecretStore` reads from `IConfiguration`; the other defaults take their settings as
method arguments. The config keys that *select* a provider live in the adapter packages and are
driven by the composition root (see `samples/AGTec.Reference`):

| Key | Values | Default |
| --- | --- | --- |
| `AGTec:Secrets:Provider` | `Configuration` \| `AzureKeyVault` \| `Dapr` | `Configuration` |
| `AGTec:Storage:Provider` | `InMemory` \| `FileSystem` \| `AzureBlob` | `InMemory` |
| `AGTec:Storage:Scanner` | `None` \| `ClamAv` | `None` |
| `AGTec:Storage:FileSystem:Root` | filesystem path | `{BaseDirectory}/local-data/blobs` |
| `Secrets:{name}` / `{name}` | secret value (read by `ConfigurationSecretStore`) | — |

## Swapping adapters

Each port is resolved from DI, so the composition root reads a config key and registers one
implementation; consumers never change. For example:

```csharp
var provider = config["AGTec:Storage:Provider"] ?? "InMemory";
if (provider.Equals("AzureBlob", StringComparison.OrdinalIgnoreCase))
    services.AddAzureBlobStorage(config.GetConnectionString("blobs")!);  // AGTec.Platform.Azure
else
    services.AddInMemoryStorage();                                        // default
```

The same pattern selects `ISecretStore` (`AddAzureKeyVaultSecrets` / `AddDaprSecrets`) and
`IMalwareScanner` (`AddClamAvScanner`). Swapping a provider is a config change, not a recompile.

## See also

`AGTec.Platform.Azure` (Key Vault + Blob), `AGTec.Platform.ClamAv` (malware scanning),
`AGTec.Platform.Dapr` (secrets via Dapr).
