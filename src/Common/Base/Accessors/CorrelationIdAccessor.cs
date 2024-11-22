using System;
using System.Threading;

namespace AGTec.Common.Base.Accessors;

public static class CorrelationIdAccessor
{
    private static readonly AsyncLocal<Guid> _correlationId = new();

    public static Guid CorrelationId
    {
        get => _correlationId.Value;
        set => _correlationId.Value = value;
    }
}