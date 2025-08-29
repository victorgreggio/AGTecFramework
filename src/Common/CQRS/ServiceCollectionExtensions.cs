using AGTec.Common.CQRS.Dispatchers;
using Microsoft.Extensions.DependencyInjection;

namespace AGTec.Common.CQRS;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCQRS(this IServiceCollection services)
    {
        services.AddTransient<ICommandDispatcher, CommandDispatcher>();
        services.AddTransient<IQueryDispatcher, QueryDispatcher>();

        return services;
    }
}