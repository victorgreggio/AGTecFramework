namespace AGTec.Domain;

/// <summary>
/// Base class for aggregate roots — the consistency boundary and the only entry point
/// for mutating an aggregate. Enforces invariants through its own methods (never public
/// setters) and records <see cref="IDomainEvent"/>s to be dispatched when the unit of
/// work commits.
/// </summary>
/// <typeparam name="TId">The aggregate's identity type.</typeparam>
public abstract class AggregateRoot<TId> : Entity<TId>
    where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = [];

    protected AggregateRoot(TId id)
        : base(id) { }

    /// <summary>Domain events raised since the aggregate was loaded or last cleared.</summary>
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>Optimistic-concurrency token; mapped to a row version by the persistence layer.</summary>
    public int Version { get; protected set; }

    /// <summary>Records a domain event to be published when the transaction commits.</summary>
    protected void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    /// <summary>Clears recorded events; called by the infrastructure after dispatch.</summary>
    public void ClearDomainEvents() => _domainEvents.Clear();
}
