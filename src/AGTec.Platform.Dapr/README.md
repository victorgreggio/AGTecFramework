# AGTec.Platform.Dapr

Dapr adapter for the `AGTec.Platform` `ISecretStore` port. `DaprSecretStore` reads secrets through
the Dapr secrets building block via a sidecar; the actual backend (Key Vault, HashiCorp Vault, local
file, …) is chosen in the Dapr component YAML, not in code. A spike demonstrating Dapr as an optional
provider behind the AGTec ports.

Part of [AGTecFramework](https://www.nuget.org/packages?q=AGTecFramework).

## Install

```bash
dotnet add package AGTec.Platform.Dapr
```

## What it provides

- `DaprSecretStore` — `ISecretStore` over `DaprClient.GetSecretAsync(storeName, name)`. Returns the
  value keyed on the secret name (falling back to the first value); a `DaprException` yields `null`.
- DI helper on `AGTecDaprExtensions`: `AddDaprSecrets(storeName)` — registers a `DaprClient` (reading
  `DAPR_GRPC_PORT` / `DAPR_HTTP_PORT` to reach the sidecar) and the secret store.

## Usage

```csharp
using AGTec.Platform.Dapr;
using Microsoft.Extensions.DependencyInjection;

// ISecretStore -> Dapr secret store component named "secretstore"
services.AddDaprSecrets("secretstore");
```

The Dapr sidecar must be running (injected in AKS, or run via the Aspire AppHost locally), and a
secret store component with the given name must be configured.

## Configuration

`AddDaprSecrets` takes the component name directly; the keys below are how the reference composition
root (`samples/AGTec.Reference`) selects and feeds it.

| Key | Purpose | Default |
| --- | --- | --- |
| `AGTec:Secrets:Provider` = `Dapr` | selects `DaprSecretStore` | `Configuration` |
| `AGTec:Secrets:DaprStoreName` | Dapr secret store component name | `secretstore` |

## Swapping adapters

Register this in place of the default `AddConfigurationSecrets()` — one line, consumers unchanged:

```csharp
// default: services.AddConfigurationSecrets();  ->
services.AddDaprSecrets(config["AGTec:Secrets:DaprStoreName"] ?? "secretstore");
```

The composition root picks it when `AGTec:Secrets:Provider=Dapr`; the concrete secret backend then
lives in the Dapr component YAML, so swapping either is a config change, not a recompile.

## See also

`AGTec.Platform` (the `ISecretStore` port), `AGTec.Platform.Azure` (direct Key Vault adapter),
`AGTec.Platform.ClamAv`.
