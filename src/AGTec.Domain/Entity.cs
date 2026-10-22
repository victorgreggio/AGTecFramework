namespace AGTec.Domain;

/// <summary>
/// Base class for entities: objects with a stable identity whose equality is defined
/// by that identity, not by their attributes. The identity is set once at construction
/// and is immutable thereafter.
/// </summary>
/// <typeparam name="TId">The identity type (e.g. a strongly-typed id or <see cref="Guid"/>).</typeparam>
public abstract class Entity<TId> : IEquatable<Entity<TId>>
    where TId : notnull
{
    protected Entity(TId id)
    {
        ArgumentNullException.ThrowIfNull(id);
        Id = id;
    }

    /// <summary>The entity's stable, immutable identity.</summary>
    public TId Id { get; }

    public bool Equals(Entity<TId>? other) =>
        other is not null && GetType() == other.GetType() && EqualityComparer<TId>.Default.Equals(Id, other.Id);

    public override bool Equals(object? obj) => obj is Entity<TId> other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);

    public static bool operator ==(Entity<TId>? left, Entity<TId>? right) => Equals(left, right);

    public static bool operator !=(Entity<TId>? left, Entity<TId>? right) => !Equals(left, right);
}
