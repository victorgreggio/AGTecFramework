using AGTec.Common.BackgroundTaskQueue;
using AGTec.Common.Monitor;
using AGTec.Microservice.Auth.Configuration;
using AGTec.Microservice.Auth.Handlers;
using AGTec.Microservice.Auth.Providers;
using AGTec.Microservice.Cache;
using AGTec.Microservice.Filters;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using Azure.Monitor.OpenTelemetry.Exporter;
using Correlate.DependencyInjection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AGTec.Microservice;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAGTecMicroservice(this IServiceCollection services,
        IConfiguration configuration, IHostEnvironment hostEnv)
    {
        services.AddCommonAGTecMicroservice(configuration, hostEnv);
        services.AddHealthChecks();
        return services;
    }

    public static IServiceCollection AddAGTecMicroservice<TContext>(this IServiceCollection services,
        IConfiguration configuration, IHostEnvironment hostEnv) where TContext : DbContext
    {
        services.AddCommonAGTecMicroservice(configuration, hostEnv);
        services.AddHealthChecks().AddDbContextCheck<TContext>();
        return services;
    }

    private static IServiceCollection AddCommonAGTecMicroservice(this IServiceCollection services,
        IConfiguration configuration, IHostEnvironment hostEnv)
    {
        // InMemory Cache
        services.AddMemoryCache();
        services.AddTransient<ICacheProvider, InMemoryCacheProvider>();

        // Service Discovery
        services.AddServiceDiscovery();

        // Http Resilience
        services.ConfigureHttpClientDefaults(http =>
        {
            http.AddStandardResilienceHandler();
            http.AddServiceDiscovery();
        });

        // OpenTelemetry
        services.AddOpenTelemetry()
            .WithLogging(logging => logging.AddAzureMonitorLogExporter())
            .WithMetrics(metrics => metrics.AddAspNetCoreInstrumentation().AddHttpClientInstrumentation().AddRuntimeInstrumentation())
            .WithTracing(tracing => tracing.AddAspNetCoreInstrumentation().AddHttpClientInstrumentation())
        .UseAzureMonitor();

        // CORS
        var allowedOrigins = configuration.GetChildren().Any(child => child.Key == "AllowedOrigins")
            ? configuration.GetSection("AllowedOrigins").Get<string[]>()
            : new[] { "http://localhost", "https://localhost" };

        services.AddCors(options => options.AddPolicy("CorsPolicy", builder =>
            builder
                .AllowAnyMethod()
                .AllowAnyHeader()
                .WithOrigins(allowedOrigins)
                .AllowCredentials())
        );

        // Monitor
        services.AddAGTecMonitor(hostEnv);

        // CorrelationId
        services.AddCorrelate(options =>
            options.RequestHeaders = new[]
            {
                "X-Correlation-ID"
            });

        // Authentication / Authorization
        services.AddAuth(configuration);

        // Queued Tasks
        services.AddHostedService<QueuedHostedService>();
        services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();

        // Controllers with Action Filters
        services.AddControllersWithViews(opts =>
        {
            opts.Filters.Add<InvalidModelStateFilter>();
            opts.Filters.Add<CorrelationIdFilter>();
        });

        // OpenAPI
        services.AddOpenApi();

        return services;
    }

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