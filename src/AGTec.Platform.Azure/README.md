# AGTec.Platform.Azure

Azure adapters for the `AGTec.Platform` ports: `AzureKeyVaultSecretStore` implements `ISecretStore`,
`AzureBlobObjectStore` implements `IObjectStore`, and `AzureBlobUrlProvider` implements
`IObjectUrlProvider` (SAS-based direct upload/download). Selected by configuration in place of the
in-memory/configuration defaults.

Part of [AGTecFramework](https://www.nuget.org/packages?q=AGTecFramework).

## Install

```bash
dotnet add package AGTec.Platform.Azure
```

## What it provides

- `AzureKeyVaultSecretStore` — `ISecretStore` over Azure Key Vault (`SecretClient`); a missing
  secret (404) returns `null`.
- `AzureBlobObjectStore` — `IObjectStore` over Azure Blob Storage (`BlobServiceClient`), creating
  the container on first write and persisting the content type.
- `AzureBlobUrlProvider` — `IObjectUrlProvider` issuing short-lived Blob SAS URLs for direct client
  upload/download. Requires a client that can sign SAS (shared key / connection string), otherwise
  throws with guidance to use a user-delegation SAS under managed identity.
- DI helpers on `AGTecAzureExtensions`: `AddAzureKeyVaultSecrets(vaultUri)` (uses
  `DefaultAzureCredential`) and `AddAzureBlobStorage(connectionString)` (registers both the object
  store and the URL provider).

## Usage

```csharp
using AGTec.Platform.Azure;
using Microsoft.Extensions.DependencyInjection;

// ISecretStore -> Key Vault (Workload Identity in AKS via DefaultAzureCredential)
services.AddAzureKeyVaultSecrets(new Uri("https://my-vault.vault.azure.net/"));

// IObjectStore + IObjectUrlProvider -> Blob Storage
services.AddAzureBlobStorage(config.GetConnectionString("blobs")!);
```

## Configuration

This package exposes typed `AddX(...)` helpers; the keys below are how the reference composition
root (`samples/AGTec.Reference`) selects and feeds them.

| Key | Purpose | Default |
| --- | --- | --- |
| `AGTec:Secrets:Provider` = `AzureKeyVault` | selects `AzureKeyVaultSecretStore` | `Configuration` |
| `AGTec:Secrets:VaultUri` | Key Vault URI passed to `AddAzureKeyVaultSecrets` | — (required) |
| `AGTec:Storage:Provider` = `AzureBlob` | selects `AzureBlobObjectStore` + `AzureBlobUrlProvider` | `InMemory` |
| `ConnectionStrings:blobs` | Blob connection string for `AddAzureBlobStorage` | `UseDevelopmentStorage=true` |

## Swapping adapters

Register these in place of the `AGTec.Platform` defaults — one line each, consumers unchanged:

```csharp
// secrets: AddConfigurationSecrets()  ->
services.AddAzureKeyVaultSecrets(new Uri(config["AGTec:Secrets:VaultUri"]!));

// storage: AddInMemoryStorage()/AddFileSystemStorage(..)  ->
services.AddAzureBlobStorage(config.GetConnectionString("blobs")!);
```

The composition root picks these when `AGTec:Secrets:Provider=AzureKeyVault` /
`AGTec:Storage:Provider=AzureBlob`, so moving to Azure is a config change, not a recompile.

## See also

`AGTec.Platform` (the ports), `AGTec.Platform.ClamAv` and `AGTec.Platform.Dapr` (other adapters).
