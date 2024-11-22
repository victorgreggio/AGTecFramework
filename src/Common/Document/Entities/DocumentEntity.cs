using System;
using AGTec.Common.Domain.Entities;

namespace AGTec.Common.Document.Entities;

public abstract class DocumentEntity : Entity, IDocumentEntity
{
    protected DocumentEntity() : base(Guid.NewGuid())
    {
    }

    protected DocumentEntity(Guid id) : base(id)
    {
    }

    public int SchemaVersion { get; set; }
}