using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.SignalR;

namespace AGTec.Common.SignalR;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseAGTecSignalRInfrastructure(this IApplicationBuilder app,
        Action<HubRouteBuilder> options)
    {
        // SignalR
        app.UseSignalR(options);

        return app;
    }
}