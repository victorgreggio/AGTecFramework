using System;

namespace AGTec.Common.Domain.Entities;

public interface IEntity
{
    Guid Id { get; }

    DateTime Created { get; set; }

    DateTime LastUpdated { get; set; }

    string UpdatedBy { get; set; }

    byte[] Version { get; set; }
}