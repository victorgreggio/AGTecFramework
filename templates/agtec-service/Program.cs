using AGTec.Api;
using AGTec.Security;
using AGTec.ServiceDefaults;
using AGTecService;

var builder = WebApplication.CreateBuilder(args);

// Telemetry, health, service discovery, resilient HTTP.
builder.AddAGTecServiceDefaults();

// OIDC/JWT bearer (configure Jwt:Authority / Jwt:Audience).
builder.AddAGTecJwtBearer();

// Problem Details + native OpenAPI (with the bearer scheme).
builder.Services.AddAGTecApiDefaults();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

// Liveness (/alive) and readiness (/health) probes.
app.MapAGTecDefaultEndpoints();

// OpenAPI document + Scalar UI (/scalar) in development.
if (app.Environment.IsDevelopment())
{
    app.MapAGTecOpenApi();
}

app.MapWidgetEndpoints();

app.Run();
