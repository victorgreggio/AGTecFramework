# AGTec.Security

Authentication/authorization conventions for AGTecFramework hosts: OIDC/JWT bearer configured from
`Jwt:Authority` / `Jwt:Audience`, plus endpoint-level OAuth scope checks. Identity-provider portable —
Entra ID by default, any OIDC IdP otherwise.

Part of [AGTecFramework](https://www.nuget.org/packages?q=AGTecFramework).

## Install

```bash
dotnet add package AGTec.Security
```

## What it provides

- `AddAGTecJwtBearer(configure?)` — on an `IHostApplicationBuilder`, adds JWT bearer authentication (reading
  `Jwt:Authority` / `Jwt:Audience`), authorization, and the scope authorization handler. An optional
  `Action<JwtBearerOptions>` lets you customize the bearer options.
- `RequireScope(params string[] scopes)` — on any `IEndpointConventionBuilder`, requires an authenticated
  caller holding at least one of the given OAuth scopes.
- `ScopeRequirement` / `ScopeAuthorizationHandler` — the authorization requirement and handler, which read
  the standard `scope` / `scp` claims (space-delimited or repeated).

## Usage

```csharp
using AGTec.Security;

var builder = WebApplication.CreateBuilder(args);

// OIDC/JWT bearer (Jwt:Authority / Jwt:Audience from configuration).
builder.AddAGTecJwtBearer();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

// Every endpoint in the group requires a token carrying the "api" scope.
app.MapGroup("/forecasts")
   .RequireScope("api")
   .MapGet("/", () => Results.Ok());

app.Run();
```

## Configuration

| Key | Default | Purpose |
| --- | --- | --- |
| `Jwt:Authority` | _(unset)_ | OIDC authority (token issuer / metadata URL). Left unset means no authority is applied. |
| `Jwt:Audience` | _(unset)_ | Expected token audience. When unset, audience validation is disabled. |

## See also

`AGTec.Api` (surfaces the `"Bearer"` scheme registered here in the OpenAPI document),
`AGTec.ServiceDefaults` (host defaults).
