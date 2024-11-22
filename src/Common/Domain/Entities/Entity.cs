using System;
using System.Collections.Generic;
using AGTec.Common.Base.ValueObjects;

namespace AGTec.Common.Domain.Entities;

public abstract class Entity : ValueObject, IEntity
{
    protected Entity(Guid id)
    {
        if (Guid.Empty == id)
            throw new ArgumentNullException(nameof(id));

        Id = id;
    }

    public Guid Id { get; set; }

    public DateTime Created { get; set; }

    public DateTime LastUpdated { get; set; }

    public string UpdatedBy { get; set; }

    public byte[] Version { get; set; }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Id;
    }
}