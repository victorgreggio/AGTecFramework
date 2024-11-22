using System.Security.Principal;
using System.Threading;

namespace AGTec.Common.Base.Accessors;

public static class PrincipalAccessor
{
    private static readonly AsyncLocal<IPrincipal> _principal = new();

    public static IPrincipal Principal
    {
        get => _principal.Value;
        set => _principal.Value = value;
    }
}