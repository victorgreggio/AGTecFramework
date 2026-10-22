using AGTec.Grpc;
using AGTec.Reference.Advisory;
using AGTec.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddAGTecServiceDefaults();
builder.Services.AddAGTecGrpcServer();

var app = builder.Build();

// Liveness/readiness probes (HTTP/1.1) alongside the gRPC endpoints (HTTP/2).
app.MapAGTecDefaultEndpoints();

app.MapGrpcService<WeatherAdvisoryService>();

app.Run();

// Exposed so the gRPC integration test can host this service via WebApplicationFactory.
public partial class Program;
