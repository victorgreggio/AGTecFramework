using AGTec.Common.BackgroundTaskQueue;
using AGTec.Common.Monitor;
using AGTec.Services.ServiceDefaults.Auth.Configuration;
using AGTec.Services.ServiceDefaults.Auth.Handlers;
using AGTec.Services.ServiceDefaults.Auth.Providers;
using AGTec.Services.ServiceDefaults.Cache;
using AGTec.Services.ServiceDefaults.Filters;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using Correlate.DependencyInjection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AGTec.Services.ServiceDefaults;

public static class HostApplicationBuilderExtensions
{
    public static IHostApplicationBuilder AddServiceDefaults(this IHostApplicationBuilder builder)
    {
        builder.AddBaseServiceDefaults();
        builder.Services.AddHealthChecks().AddSelfCheck();
        return builder;
    }

    public static IHostApplicationBuilder AddServiceDefaults<TContext>(this IHostApplicationBuilder builder) where TContext : DbContext
    {
        builder.AddBaseServiceDefaults();
        builder.Services.AddHealthChecks().AddSelfCheck().AddDbContextCheck<TContext>();
        return builder;
    }

    private static IHostApplicationBuilder AddBaseServiceDefaults(this IHostApplicationBuilder builder)
    {
        // OpenTelemetry
        builder.ConfigureOpenTelemetry();

        // Service Discovery
        builder.Services.AddServiceDiscovery();

        // InMemory Cache
        builder.Services.AddMemoryCache();
        builder.Services.AddTransient<ICacheProvider, InMemoryCacheProvider>();

        // Http Resilience
        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            http.AddStandardResilienceHandler();
            http.AddServiceDiscovery();
        });

        // Monitor
        builder.Services.AddAGTecMonitor(builder.Environment);

        builder.Services.AddCorrelate(options =>
           options.RequestHeaders = new[]
           {
                "X-Correlation-ID"
           });

        // Authentication / Authorization
        builder.Services.AddAuth(builder.Configuration);

        // Queued Tasks
        builder.Services.AddHostedService<QueuedHostedService>();
        builder.Services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();

        // Controllers with Action Filters
        builder.Services.AddControllersWithViews(opts =>
        {
            opts.Filters.Add<InvalidModelStateFilter>();
            opts.Filters.Add<CorrelationIdFilter>();
        });

        // OpenAPI
        builder.Services.AddOpenApi();

        return builder;
    }

    private static IHostApplicationBuilder ConfigureOpenTelemetry(this IHostApplicationBuilder builder)
    {
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();
            })
            .WithTracing(tracing =>
            {
                tracing.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation();
            })
            .UseAzureMonitor();

        return builder;
    }

    private static IHealthChecksBuilder AddSelfCheck(this IHealthChecksBuilder builder) => builder.AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);
    private static IHealthChecksBuilder AddDbContextCheck<TContext>(this IHealthChecksBuilder builder) where TContext : DbContext
        => builder.AddDbContextCheck<TContext>("Database", HealthStatus.Unhealthy, ["ready"]);

    private static IServiceCollection AddAuth(this IServiceCollection services, IConfiguration configuration)
    {
        if (configuration.GetChildren().Any(child => child.Key.Equals(AuthConfiguration.ConfigSectionName)) == false)
            throw new Exception("Auth Configuration section not found.");

        var authConfiguration =
            configuration.GetSection(AuthConfiguration.ConfigSectionName).Get<AuthConfiguration>();

        if (authConfiguration.IsValid() == false)
            throw new Exception($"Configuration section '{AuthConfiguration.ConfigSectionName}' not found.");

        services.AddSingleton<IAuthConfiguration>(authConfiguration);

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
            .AddJwtBearer(options =>
            {
                options.Authority = authConfiguration.AuthorityIdentity;
                options.Audience = authConfiguration.Audience;
                options.SaveToken = true;
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

        services.AddSingleton<IAuthorizationPolicyProvider, AuthorizationPolicyProvider>();
        services.AddSingleton<IAuthorizationHandler, ScopeAuthorizationHandler>();
        services.AddSingleton<IAuthorizationHandler, ClaimOrScopeAuthorizationHandler>();
        services.AddSingleton<IAuthorizationHandler, ClaimAuthorizationHandler>();
        services.AddSingleton<IAuthorizationHandler, ClaimValueAuthorizationHandler>();

        return services;
    }
}

