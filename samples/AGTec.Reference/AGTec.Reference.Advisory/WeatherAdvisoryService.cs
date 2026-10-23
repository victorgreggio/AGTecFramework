using AGTec.Reference.Grpc.Contracts;
using Grpc.Core;

namespace AGTec.Reference.Advisory;

/// <summary>
/// gRPC implementation of the Advisory contract. A small, synchronous internal capability —
/// exactly the kind of call that is clearer as gRPC request/response than as an async event.
/// </summary>
public sealed partial class WeatherAdvisoryService(ILogger<WeatherAdvisoryService> logger)
    : AdvisoryService.AdvisoryServiceBase
{
    public override Task<AdvisoryReply> GetAdvisory(AdvisoryRequest request, ServerCallContext context)
    {
        LogAdvisoryRequested(logger, request.Celsius);

        var advice = request.Celsius switch
        {
            <= 0 => "Freezing — heavy coat, gloves, and watch for ice.",
            <= 12 => "Cold — wear a warm jacket.",
            <= 22 => "Mild — a light layer is plenty.",
            <= 30 => "Warm — stay hydrated.",
            _ => "Hot — seek shade and drink water.",
        };

        return Task.FromResult(new AdvisoryReply { Advice = advice });
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Advisory requested for {Celsius}°C")]
    private static partial void LogAdvisoryRequested(ILogger logger, int celsius);
}
