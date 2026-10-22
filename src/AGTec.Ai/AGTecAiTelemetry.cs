using Microsoft.Extensions.Configuration;

namespace AGTec.Ai;

/// <summary>Conventions for AI (chat-client) OpenTelemetry in a AGTec app.</summary>
public static class AGTecAiTelemetry
{
    /// <summary>Default <c>ActivitySource</c> name for AI spans. <c>AGTec.ServiceDefaults</c> registers it in
    /// the tracer, so a chat client instrumented through <see cref="AGTecAiTelemetryExtensions.WithTelemetry"/>
    /// with the default source is captured with no extra wiring.</summary>
    public const string SourceName = "AGTec.Ai";
}

/// <summary>
/// AI telemetry toggles. A chat client wrapped with <see cref="AGTecAiTelemetryExtensions.WithTelemetry"/> is
/// always instrumented — spans carry the model, token usage, latency, and finish reason. <see cref="CaptureContent"/>
/// additionally records the <em>message content</em> (the prompts and responses — the model's reasoning). That
/// content can be sensitive (user input, retrieved documents, generated text), so it defaults to <c>false</c> and
/// should stay off when deployed; turn it on locally to inspect full reasoning in a trace viewer.
/// </summary>
public sealed record AGTecAiTelemetryOptions(bool CaptureContent = false)
{
    /// <summary>Configuration key that enables content capture: <c>AGTec:Ai:CaptureContent</c>.</summary>
    public const string CaptureContentKey = "AGTec:Ai:CaptureContent";

    /// <summary>Builds the options from configuration (reads <see cref="CaptureContentKey"/>; default off).</summary>
    public static AGTecAiTelemetryOptions FromConfiguration(IConfiguration configuration) =>
        new(bool.TryParse(configuration[CaptureContentKey], out var capture) && capture);
}
