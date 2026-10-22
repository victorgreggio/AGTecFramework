namespace AGTec.Messaging;

/// <summary>
/// Marks a message that crosses a service boundary: a versioned, published fact other
/// bounded contexts may subscribe to. Integration events travel via the broker and the
/// transactional outbox — never a leaked CLR type. Keep them stable and additive.
/// </summary>
public interface IIntegrationEvent
{
    /// <summary>Stable unique id for the message, used for idempotent (de-duplicated) consumption.</summary>
    Guid MessageId { get; }

    /// <summary>When the originating fact occurred (UTC).</summary>
    DateTimeOffset OccurredOn { get; }
}
