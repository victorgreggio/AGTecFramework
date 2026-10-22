using Microsoft.Extensions.DependencyInjection;
using nClam;
using AGTec.Platform;

namespace AGTec.Platform.ClamAv;

/// <summary>Registration helper for the ClamAV malware scanner.</summary>
public static class AGTecClamAvExtensions
{
    public static IServiceCollection AddClamAvScanner(this IServiceCollection services, string host, int port = 3310)
    {
        services.AddSingleton<IClamClient>(new ClamClient(host, port));
        services.AddSingleton<IMalwareScanner, ClamAvMalwareScanner>();
        return services;
    }
}
