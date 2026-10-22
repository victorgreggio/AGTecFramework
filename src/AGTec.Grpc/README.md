# AGTec.Grpc

gRPC conventions for internal service-to-service calls in AGTecFramework. Async events stay the default
integration style; gRPC (HTTP/2, contract-first Protobuf) is for genuinely synchronous request/response
calls. Telemetry flows automatically via the OpenTelemetry instrumentation wired by `AGTec.ServiceDefaults`.

Part of [AGTecFramework](https://www.nuget.org/packages?q=AGTecFramework).

## Install

```bash
dotnet add package AGTec.Grpc
```

## What it provides

- `AddAGTecGrpcServer()` — on an `IServiceCollection`, registers the gRPC server with AGTec defaults
  (detailed errors enabled). Returns the `IGrpcServerBuilder` for further configuration.
- `AddAGTecGrpcClient<TClient>(address)` — on an `IServiceCollection`, registers a typed gRPC client whose
  address is resolved through service discovery (e.g. `"http://advisory"`) and that inherits the standard
  HTTP resilience from `AGTec.ServiceDefaults`.

## Usage

Server host:

```csharp
using AGTec.Grpc;
using AGTec.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);
builder.AddAGTecServiceDefaults();
builder.Services.AddAGTecGrpcServer();

var app = builder.Build();
app.MapGrpcService<WeatherAdvisoryService>();
app.Run();
```

Client host (service-discovery address):

```csharp
using AGTec.Grpc;

// Resolves "http://advisory" via service discovery; inject the typed client where needed.
builder.Services.AddAGTecGrpcClient<AdvisoryService.AdvisoryServiceClient>("http://advisory");
```

## See also

`AGTec.ServiceDefaults` (service discovery, HTTP resilience, and OpenTelemetry this package relies on).
