using System;

namespace AGTec.Common.Repository;

public interface ITrackingChangeRepository : IDisposable
{
    bool AutoDetectChanges { get; set; }
}