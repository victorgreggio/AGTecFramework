# AGTec.Api

API edge conventions for AGTecFramework web hosts: native ASP.NET Core OpenAPI (with the JWT bearer scheme)
and RFC 9457 Problem Details. Standardizes on `Microsoft.AspNetCore.OpenApi` + Scalar — no Swashbuckle.

Part of [AGTecFramework](https://www.nuget.org/packages?q=AGTecFramework).

## Install

```bash
dotnet add package AGTec.Api
```

## What it provides

- `AddAGTecApiDefaults()` — on an `IServiceCollection`, registers Problem Details for all error responses and
  the native OpenAPI document, with a transformer that adds the JWT bearer security scheme.
- `MapAGTecOpenApi()` — on a `WebApplication`, maps the OpenAPI document and the Scalar API reference UI at
  `/scalar`.

The bearer security scheme is added to the generated document only when a `"Bearer"` authentication scheme
is actually registered (e.g. by `AGTec.Security`), so the "Authorize" affordance appears exactly when tokens
are expected.

## Usage

```csharp
using AGTec.Api;

var builder = WebApplication.CreateBuilder(args);

// Problem Details + native OpenAPI (with the JWT bearer scheme).
builder.Services.AddAGTecApiDefaults();

var app = builder.Build();

// OpenAPI document + Scalar API reference UI (/scalar) — typically dev-only.
if (app.Environment.IsDevelopment())
{
    app.MapAGTecOpenApi();
}

app.Run();
```

## See also

`AGTec.ServiceDefaults` (telemetry, health, resilience), `AGTec.Security` (registers the `"Bearer"` scheme
this package documents in OpenAPI).
