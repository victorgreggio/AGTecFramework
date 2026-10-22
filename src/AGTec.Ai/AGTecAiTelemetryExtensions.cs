using System.Linq;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AGTec.Ai;

/// <summary>
/// OpenTelemetry for <see cref="IChatClient"/>: instruments chat calls (model, token usage, latency, finish
/// reason) and, when enabled, records prompt/response content. The capture toggle defaults to off so sensitive
/// content is not written to traces unless a deployment opts in.
/// </summary>
public static class AGTecAiTelemetryExtensions
{
    /// <summary>Registers <see cref="AGTecAiTelemetryOptions"/> from configuration (<c>AGTec:Ai:CaptureContent</c>),
    /// so chat clients and app code can resolve it. Call once at startup.</summary>
    public static IServiceCollection AddAGTecAiTelemetry(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(AGTecAiTelemetryOptions.FromConfiguration(configuration));
        return services;
    }

    /// <summary>
    /// Wraps a chat client with OpenTelemetry. Spans always carry the model, token usage, latency, and finish
    /// reason; message content is recorded only when <see cref="AGTecAiTelemetryOptions.CaptureContent"/> is set.
    /// Use this for clients the app constructs itself (e.g. a per-intent resolver); pass a <paramref name="sourceName"/>
    /// to group the spans under the app's own source, otherwise <see cref="AGTecAiTelemetry.SourceName"/> is used.
    /// </summary>
    public static IChatClient WithTelemetry(
        this IChatClient client,
        AGTecAiTelemetryOptions options,
        string? sourceName = null) =>
        client
            .AsBuilder()
            .UseOpenTelemetry(
                sourceName: sourceName ?? AGTecAiTelemetry.SourceName,
                configure: c => c.EnableSensitiveData = options.CaptureContent)
            .Build();

    /// <summary>
    /// Decorates the already-registered <see cref="IChatClient"/> with telemetry (see <see cref="WithTelemetry"/>).
    /// Register an <see cref="IChatClient"/> and call <see cref="AddAGTecAiTelemetry"/> first.
    /// </summary>
    public static IServiceCollection AddChatClientTelemetry(this IServiceCollection services, string? sourceName = null)
    {
        var descriptor =
            services.LastOrDefault(d => d.ServiceType == typeof(IChatClient))
            ?? throw new InvalidOperationException("Register an IChatClient before adding chat-client telemetry.");

        services.Remove(descriptor);
        services.Add(
            new ServiceDescriptor(
                typeof(IChatClient),
                provider =>
                {
                    var inner = (IChatClient)(
                        descriptor.ImplementationInstance
                        ?? descriptor.ImplementationFactory?.Invoke(provider)
                        ?? ActivatorUtilities.CreateInstance(provider, descriptor.ImplementationType!));
                    var options = provider.GetService<AGTecAiTelemetryOptions>() ?? new AGTecAiTelemetryOptions();
                    return inner.WithTelemetry(options, sourceName);
                },
                descriptor.Lifetime));

        return services;
    }
}
