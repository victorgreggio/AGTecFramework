# AGTec.FeatureManagement.Azure

Azure App Configuration adapter for AGTecFramework feature flags. Adds App Configuration as a configuration
source with feature flags and **dynamic refresh**, so `AGTec.FeatureManagement` serves cloud-managed flags
that flip at runtime without a redeploy. App Configuration's flags land in the standard `FeatureManagement`
section, so nothing about how you read flags changes.

Part of [AGTecFramework](https://www.nuget.org/packages?q=AGTecFramework).

## Install

```bash
dotnet add package AGTec.FeatureManagement.Azure
```

## What it provides

- `AddAGTecAzureAppConfiguration(refreshInterval?)` — on an `IHostApplicationBuilder`, adds App Configuration
  as a configuration source with feature flags and polling refresh. Authenticates with
  `ConnectionStrings:appconfig` when present, otherwise `DefaultAzureCredential` against
  `AGTec:FeatureFlags:AppConfig:Endpoint` (managed identity in AKS, developer credentials locally).
- `UseAGTecAzureAppConfiguration()` — the refresh middleware; place it early in the pipeline.

## Usage

```csharp
using AGTec.FeatureManagement;
using AGTec.FeatureManagement.Azure;

// Select the provider by config (Configuration by default; AzureAppConfiguration when deployed).
if (builder.Configuration["AGTec:FeatureFlags:Provider"] == "AzureAppConfiguration")
{
    builder.AddAGTecAzureAppConfiguration();
}

builder.Services.AddAGTecTenancy();
builder.Services.AddAGTecFeatureManagement();

var app = builder.Build();
app.UseAGTecAzureAppConfiguration(); // picks up flag changes at runtime
```

## Configuration

| Key | Purpose | Default |
| --- | --- | --- |
| `AGTec:FeatureFlags:Provider` = `AzureAppConfiguration` | selects this adapter (vs the default `Configuration`) | `Configuration` |
| `AGTec:FeatureFlags:AppConfig:Endpoint` | App Configuration endpoint (used with `DefaultAzureCredential`) | — |
| `ConnectionStrings:appconfig` | App Configuration connection string (used instead of the endpoint) | — |

Manage the flags and their filters (targeting, percentage, time window) in the App Configuration resource;
they refresh into the running app on the polling interval (30s by default).

## See also

`AGTec.FeatureManagement` (the flag API and per-tenant targeting this feeds).
