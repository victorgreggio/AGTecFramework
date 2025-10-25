using AGTec.Services.ServiceDefaults.Authentication;
using AGTec.Services.ServiceDefaults.Authentication.Handlers;
using AGTec.Services.ServiceDefaults.Authentication.Providers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Threading.Tasks;

namespace AGTec.Services.ServiceDefaults;

internal static class AuthenticationExtensions
{
    public static IHostApplicationBuilder AddJwtBearerAuthentication(this IHostApplicationBuilder builder)
    {
        var authConfig = builder.Configuration.GetSection("Authentication").Get<AuthenticationConfiguration>()
            ?? throw new InvalidOperationException("Authentication configuration is missing or invalid.");
        builder.Services.AddSingleton<IAuthenticationConfiguration>(authConfig);

        if (!authConfig.IsValid())
        {
            throw new InvalidOperationException("Authentication configuration validation failed. Ensure Authority and Audience are properly configured.");
        }

        var requireHttpsMetadata = authConfig.RequireHttpsMetadata && !builder.Environment.IsDevelopment();

        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = authConfig.Authority;
                options.Audience = authConfig.Audience;
                options.RequireHttpsMetadata = requireHttpsMetadata;
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateAudience = true,
                    ValidateIssuer = true,
                    ValidateLifetime = true,
                    ValidIssuer = authConfig.Authority,
                    ValidAudience = authConfig.Audience
                };
                options.Events = new JwtBearerEvents
                {
                    // Needed for SignalR when using Websocket Protocol
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];

                        if (string.IsNullOrWhiteSpace(accessToken) == false) context.Token = accessToken;

                        return Task.CompletedTask;
                    }
                };
            });

        builder.Services.AddAuthorization();

        builder.Services.AddSingleton<IAuthorizationPolicyProvider, AuthorizationPolicyProvider>();
        builder.Services.AddSingleton<IAuthorizationHandler, ScopeAuthorizationHandler>();
        builder.Services.AddSingleton<IAuthorizationHandler, ClaimOrScopeAuthorizationHandler>();
        builder.Services.AddSingleton<IAuthorizationHandler, ClaimAuthorizationHandler>();
        builder.Services.AddSingleton<IAuthorizationHandler, ClaimValueAuthorizationHandler>();

        return builder;
    }
}
