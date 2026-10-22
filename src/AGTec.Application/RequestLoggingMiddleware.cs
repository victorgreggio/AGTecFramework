using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Wolverine;

namespace AGTec.Application;

/// <summary>
/// Wolverine middleware that logs every message handled, with timing. This is the kind
/// of cross-cutting concern that used to be copy-pasted into each handler; as a pipeline
/// behavior it runs once, around all of them. Validation, transactions, and idempotency
/// compose the same way. Uses source-generated logging for zero-allocation on the hot path.
/// </summary>
public static partial class RequestLoggingMiddleware
{
    public static Stopwatch Before(Envelope envelope, ILogger<Envelope> logger)
    {
        var messageType = envelope.Message?.GetType().Name;
        LogHandling(logger, messageType, envelope.Id);
        return Stopwatch.StartNew();
    }

    public static void Finally(Stopwatch timer, Envelope envelope, ILogger<Envelope> logger)
    {
        timer.Stop();
        var messageType = envelope.Message?.GetType().Name;
        LogHandled(logger, messageType, envelope.Id, timer.ElapsedMilliseconds);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Handling {MessageType} ({MessageId})")]
    private static partial void LogHandling(ILogger logger, string? messageType, Guid messageId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Handled {MessageType} ({MessageId}) in {ElapsedMs} ms")]
    private static partial void LogHandled(ILogger logger, string? messageType, Guid messageId, long elapsedMs);
}
