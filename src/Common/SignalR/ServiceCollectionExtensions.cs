using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;

namespace AGTec.Common.SignalR;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAGTecSignalRInfrastructure(this IServiceCollection services)
    {
        // Add UserID Provider
        services.AddSingleton<IUserIdProvider, UserIdProvider>();

        // SignalR
        services.AddSignalR();

        return services;
    }
}