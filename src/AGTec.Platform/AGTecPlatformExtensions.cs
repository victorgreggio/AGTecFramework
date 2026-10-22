using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace AGTec.Platform;

/// <summary>Registration helpers for the default (no-cloud) platform adapters.</summary>
public static class AGTecPlatformExtensions
{
    public static IServiceCollection AddConfigurationSecrets(this IServiceCollection services) =>
        services.AddSingleton<ISecretStore, ConfigurationSecretStore>();

    public static IServiceCollection AddInMemoryStorage(this IServiceCollection services) =>
        services.AddSingleton<IObjectStore, InMemoryObjectStore>();

    /// <summary>Durable filesystem object store (local default) rooted at <paramref name="root"/>.</summary>
    public static IServiceCollection AddFileSystemStorage(this IServiceCollection services, string root) =>
        services.AddSingleton<IObjectStore>(_ => new FileSystemObjectStore(root));

    /// <summary>No-op scanner — use when malware scanning happens at the storage layer.</summary>
    public static IServiceCollection AddNoOpMalwareScanner(this IServiceCollection services) =>
        services.AddSingleton<IMalwareScanner, NoOpMalwareScanner>();

    /// <summary>
    /// Wraps the registered <see cref="IObjectStore"/> in a <see cref="MalwareScanningObjectStore"/>
    /// so uploads are scanned before they are stored. Register an <see cref="IObjectStore"/> and an
    /// <see cref="IMalwareScanner"/> first.
    /// </summary>
    public static IServiceCollection ScanObjectStoreForMalware(this IServiceCollection services) =>
        services.Decorate<IObjectStore>(
            (inner, provider) => new MalwareScanningObjectStore(inner, provider.GetRequiredService<IMalwareScanner>())
        );

    /// <summary>
    /// Replaces the last registration of <typeparamref name="TService"/> with a decorator that
    /// wraps it — the standard "decorate a DI service" move, without an extra dependency.
    /// </summary>
    public static IServiceCollection Decorate<TService>(
        this IServiceCollection services,
        Func<TService, IServiceProvider, TService> decorator
    )
        where TService : class
    {
        var descriptor =
            services.LastOrDefault(d => d.ServiceType == typeof(TService))
            ?? throw new InvalidOperationException(
                $"{typeof(TService).Name} must be registered before it can be decorated."
            );

        services.Remove(descriptor);

        Func<IServiceProvider, object> createInner = descriptor switch
        {
            { ImplementationInstance: { } instance } => _ => instance,
            { ImplementationFactory: { } factory } => factory,
            { ImplementationType: { } type } => provider => ActivatorUtilities.CreateInstance(provider, type),
            _ => throw new InvalidOperationException("Unsupported service descriptor for decoration."),
        };

        services.Add(
            new ServiceDescriptor(
                typeof(TService),
                provider => decorator((TService)createInner(provider), provider),
                descriptor.Lifetime
            )
        );

        return services;
    }
}
