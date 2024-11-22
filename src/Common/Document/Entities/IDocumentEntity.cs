using AGTec.Common.Domain.Entities;

namespace AGTec.Common.Document.Entities;

public interface IDocumentEntity : IEntity
{
    int SchemaVersion { get; set; }
}