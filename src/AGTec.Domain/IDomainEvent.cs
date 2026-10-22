namespace AGTec.Domain;

/// <summary>
/// Marks an in-process domain event raised by an aggregate <b>inside</b> its transaction.
/// Distinct from an integration event, which crosses a service boundary via the broker
/// and the transactional outbox. Keep domain events internal to the bounded context.
/// </summary>
public interface IDomainEvent
{
    /// <summary>When the event occurred (UTC).</summary>
    DateTimeOffset OccurredOn { get; }
}
