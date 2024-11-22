using System;

namespace AGTec.Common.Repository;

public sealed class NoTrackingChangeContext : IDisposable
{
    private readonly bool _initialValue;
    private readonly ITrackingChangeRepository _repository;

    public NoTrackingChangeContext(ITrackingChangeRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));

        _initialValue = _repository.AutoDetectChanges;

        _repository.AutoDetectChanges = false;
    }

    public void Dispose()
    {
        _repository.AutoDetectChanges = _initialValue;
    }
}