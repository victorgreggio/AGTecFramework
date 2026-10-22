# AGTec.FeatureManagement

Feature flags for AGTecFramework on **`Microsoft.FeatureManagement`** — config-driven flags with the
built-in filters (Percentage, TimeWindow, Targeting) and a **tenant-aware targeting accessor**, so
percentage rollouts and targeted audiences evaluate **per tenant** out of the box.

Part of [AGTecFramework](https://www.nuget.org/packages?q=AGTecFramework).

## Install

```bash
dotnet add package AGTec.FeatureManagement
```

## What it provides

- `AddAGTecFeatureManagement()` — registers `Microsoft.FeatureManagement` reading the `FeatureManagement`
  configuration section, with the standard filters and AGTec's per-tenant targeting. Requires
  `AddAGTecTenancy()` for the targeting to resolve a tenant.
- `IVariantFeatureManager` / `IFeatureManager` — inject and call `IsEnabledAsync("Feature")`; or gate an
  endpoint/controller with `[FeatureGate("Feature")]` (from `Microsoft.FeatureManagement.Mvc`).
- `AGTecTenantTargetingContextAccessor` — maps the current `ITenantContext` into the targeting context: the
  tenant id is a target **group** and the workspace (or tenant) id is the stable **user id** used for
  consistent percentage bucketing.

## Usage

```csharp
using AGTec.FeatureManagement;
using Microsoft.FeatureManagement;

builder.Services.AddAGTecTenancy();
builder.Services.AddAGTecFeatureManagement();

// ... after app.UseAGTecTenancy():
app.MapGet("/tenants/{tenantId}/report", async (IVariantFeatureManager features, CancellationToken ct) =>
    await features.IsEnabledAsync("NewReport", ct) ? Results.Ok("new") : Results.Ok("classic"));
```

Flags live in the `FeatureManagement` configuration section. Target a rollout at specific tenants (the
`Groups`) or a stable percentage per tenant:

```json
{
  "FeatureManagement": {
    "NewReport": {
      "EnabledFor": [
        {
          "Name": "Targeting",
          "Parameters": {
            "Audience": {
              "Groups": [ { "Name": "acme", "RolloutPercentage": 100 } ],
              "DefaultRolloutPercentage": 25
            }
          }
        }
      ]
    }
  }
}
```

Here `acme` is fully on; every other tenant is bucketed at 25% — and because the bucket key is the tenant,
a tenant stays on the same side of the rollout across requests.

## Configuration

| Key | Purpose |
| --- | --- |
| `FeatureManagement` | The flags section (boolean flags or `{ "EnabledFor": [ … ] }` filter configs). |

The source is configuration by default. To serve cloud-managed flags that flip at runtime without a
redeploy, add **`AGTec.FeatureManagement.Azure`** (Azure App Configuration) — it feeds the same section.

## See also

`AGTec.FeatureManagement.Azure` (App Configuration source), `AGTec.Tenancy` (the tenant context targeting
keys off).
