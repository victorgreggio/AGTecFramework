using Grpc.AspNetCore.Server;
using Microsoft.Extensions.DependencyInjection;

namespace AGTec.Grpc;

/// <summary>
/// gRPC conventions for internal service-to-service calls. Async events remain the default
/// integration style; gRPC (HTTP/2, contract-first Protobuf) is for the genuinely
/// synchronous request/response calls (§7). Telemetry flows automatically via the
/// OpenTelemetry instrumentation already wired by <c>AGTec.ServiceDefaults</c>.
/// </summary>
public static class AGTecGrpcExtensions
{
    /// <summary>gRPC server defaults for a AGTec host.</summary>
    public static IGrpcServerBuilder AddAGTecGrpcServer(this IServiceCollection services) =>
        services.AddGrpc(options =>
        {
            // Surface handler exceptions to callers in non-production environments.
            options.EnableDetailedErrors = true;
        });

    /// <summary>
    /// Registers a typed gRPC client that resolves its address through service discovery
    /// (e.g. "http://advisory") and inherits the standard HTTP resilience configured by
    /// <c>AGTec.ServiceDefaults</c>.
    /// </summary>
    public static IHttpClientBuilder AddAGTecGrpcClient<TClient>(this IServiceCollection services, string address)
        where TClient : class =>
        services.AddGrpcClient<TClient>(options => options.Address = new Uri(address)).AddServiceDiscovery();
}
