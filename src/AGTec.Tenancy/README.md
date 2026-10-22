# AGTec.Tenancy

Request-scoped multi-tenancy for ASP.NET Core. A `ITenantContext` is resolved per request from the
route and validated against the caller's token claim (403 on mismatch), so every data path (DB session,
vector store, object store) can key off one tenant and a request can never touch another tenant's data.

Part of [AGTecFramework](https://www.nuget.org/packages?q=AGTecFramework).

## Install

```bash
dotnet add package AGTec.Tenancy
```

## What it provides

- `ITenantContext` — the tenant the current request is scoped to: `HasTenant`, `TenantId` (throws if
  no tenant is in scope), and optional `WorkspaceId`. Registered as a scoped service.
- `AddAGTecTenancy(configure?)` — registers the scoped `ITenantContext` and binds `AGTecTenancyOptions`.
- `UseAGTecTenancy()` — the middleware that reads the route, runs the claim check, and populates the
  context for the request.
- `AGTecTenancyOptions` — route parameter names and the claim type used for validation.

## Usage

```csharp
using AGTec.Tenancy;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAGTecTenancy(o =>
{
    o.RouteParameterName = "tenantId";   // matches /tenants/{tenantId}/...
    o.ClaimType = "tenant";              // route tenant is checked against this claim
});

var app = builder.Build();

app.UseRouting();
app.UseAuthentication();   // claim must exist before the tenant check
app.UseAuthorization();
app.UseAGTecTenancy();      // after UseRouting (route values) and UseAuthentication (claim)

app.MapGet("/tenants/{tenantId}/forecasts", (ITenantContext tenant) =>
    Results.Ok(new { tenant.TenantId, tenant.WorkspaceId }));

app.Run();
```

Middleware ordering matters: `UseAGTecTenancy()` needs route values (so it runs after `UseRouting`) and
the caller's claim (so it runs after `UseAuthentication`). When the route tenant and the `tenant` claim
disagree for an authenticated caller, the request short-circuits with **403 Forbidden**.

## Configuration

`AGTecTenancyOptions` (passed to `AddAGTecTenancy`):

| Key | Default | Purpose |
|-----|---------|---------|
| `RouteParameterName` | `"tenantId"` | Route parameter carrying the tenant id, e.g. `/tenants/{tenantId}/...`. |
| `WorkspaceRouteParameterName` | `null` | Optional route parameter carrying the workspace id; populates `ITenantContext.WorkspaceId`. |
| `ClaimType` | `"tenant"` | Claim the route tenant is validated against for authenticated callers. A mismatch returns 403. Set to `null` to skip validation (e.g. local dev). |

The tenant is only resolved when the route parameter is present and non-empty; the claim check runs
only when `ClaimType` is set and the caller is authenticated.

## See also

`AGTec.Security` (authentication/claims), `AGTec.Api` (endpoint conventions) — the other request-pipeline
building blocks.
