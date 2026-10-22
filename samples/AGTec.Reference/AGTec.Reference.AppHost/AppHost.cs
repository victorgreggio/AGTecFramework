// Aspire AppHost: one command (`dotnet run`) spins up Postgres + RabbitMQ and both
// services locally, with a dashboard, service discovery, and injected connection strings.
var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres").WithDataVolume();

var forecastsDb = postgres.AddDatabase("forecasts");

var messaging = builder.AddRabbitMQ("messaging");

var search = builder.AddElasticsearch("search");

// Internal gRPC service, reached by the API over service discovery ("http://advisory").
var advisory = builder.AddProject<Projects.AGTec_Reference_Advisory>("advisory");

builder
    .AddProject<Projects.AGTec_Reference_Api>("api")
    .WithReference(forecastsDb)
    .WaitFor(forecastsDb)
    .WithReference(messaging)
    .WaitFor(messaging)
    .WithReference(search)
    .WaitFor(search)
    .WithReference(advisory)
    .WaitFor(advisory)
    .WithEnvironment("AGTec__Search__Provider", "Elasticsearch")
    // Dapr spike (ADR-001): Aspire injects a daprd sidecar next to the API when the Dapr CLI
    // is installed. Set AGTec__Secrets__Provider=Dapr to resolve ISecretStore through it.
    .WithDaprSidecar();

builder
    .AddProject<Projects.AGTec_Reference_Worker>("worker")
    .WithReference(forecastsDb)
    .WaitFor(forecastsDb)
    .WithReference(messaging)
    .WaitFor(messaging)
    .WithReference(search)
    .WaitFor(search)
    .WithEnvironment("AGTec__Search__Provider", "Elasticsearch");

builder.Build().Run();
