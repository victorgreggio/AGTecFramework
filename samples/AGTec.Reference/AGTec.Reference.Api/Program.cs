using AGTec.Api;
using AGTec.Grpc;
using AGTec.Reference.Api;
using AGTec.Reference.Grpc.Contracts;
using AGTec.Reference.Infrastructure;
using AGTec.Security;
using AGTec.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

// Telemetry, health, service discovery, resilient HTTP.
builder.AddAGTecServiceDefaults();

// OIDC/JWT bearer authentication (Jwt:Authority / Jwt:Audience from config).
builder.AddAGTecJwtBearer();

// Problem Details + native OpenAPI (with the JWT bearer scheme).
builder.Services.AddAGTecApiDefaults();
builder.Services.AddExceptionHandler<ValidationExceptionHandler>();

// Internal synchronous call via gRPC (address resolved by service discovery).
builder.Services.AddAGTecGrpcClient<AdvisoryService.AdvisoryServiceClient>("http://advisory");

// Marten + Wolverine (mediator + transactional outbox). This host publishes integration
// events; the transport (RabbitMQ / Azure Service Bus) is selected by configuration.
builder.AddReferencePlatform(MessagingRole.Publisher);

var app = builder.Build();

app.UseExceptionHandler();

app.UseAuthentication();
app.UseAuthorization();

// Liveness (/alive) and readiness (/health) probes for Kubernetes (anonymous).
app.MapAGTecDefaultEndpoints();

// OpenAPI document + Scalar API reference UI (/scalar) in development.
if (app.Environment.IsDevelopment())
{
    app.MapAGTecOpenApi();
}

// Feature endpoints (Minimal APIs — the chosen edge style).
app.MapForecastEndpoints();

app.Run();
