using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using AGTec.Common.BackgroundTaskQueue;
using AGTec.Common.Monitor;
using AGTec.Microservice.Auth.Configuration;
using AGTec.Microservice.Auth.Handlers;
using AGTec.Microservice.Auth.Providers;
using AGTec.Microservice.Cache;
using AGTec.Microservice.Filters;
using AGTec.Microservice.Swagger;
using Correlate.DependencyInjection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Polly;
using Polly.Extensions.Http;

namespace AGTec.Microservice;

public static class ServiceCollectionExtensions
{
    private const string Retry = "DefaultRetryPolicy";
    private const string Tiemout = "DefaultTimeoutPolicy";

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

        // API Versioning
        services.AddApiVersioning(setup =>
        {
            setup.DefaultApiVersion = new ApiVersion(1, 0);
            setup.AssumeDefaultVersionWhenUnspecified = true;
            setup.ReportApiVersions = true;
        });

        // API Doc
        services.AddVersionedApiExplorer(
            options =>
            {
                options.GroupNameFormat = "'v'VVV";
                options.SubstituteApiVersionInUrl = true;
            });

        services.AddSwaggerGen(
            options =>
            {
                var provider = services.BuildServiceProvider()
                    .GetRequiredService<IApiVersionDescriptionProvider>();

                foreach (var description in provider.ApiVersionDescriptions)
                    options.SwaggerDoc(
                        description.GroupName,
                        new OpenApiInfo
                        {
                            Title = $"{description.ApiVersion}",
                            Version = description.ApiVersion.ToString()
                        });
                options.OperationFilter<SwaggerDefaultValues>();
            });

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

        // HTTPClient Policies
        services.AddHttpClientPolicies();

        // Queued Tasks
        services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();

        // Controllers with Action Filters
        services.AddControllersWithViews(opts =>
        {
            opts.Filters.Add<InvalidModelStateFilter>();
            opts.Filters.Add<CorrelationIdFilter>();
        }).AddNewtonsoftJson();

        // Application Insights
        if (configuration.GetChildren().Any(child => child.Key.Equals("ApplicationInsights")))
            services.AddApplicationInsightsTelemetry(configuration);

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

    private static IServiceCollection AddHttpClientPolicies(this IServiceCollection services)
    {
        var registry = services.AddPolicyRegistry();
        var retry = HttpPolicyExtensions.HandleTransientHttpError().WaitAndRetryAsync(new[]
        {
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(10)
        });

        var timeout = Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(10));

        registry.Add(Retry, retry);
        registry.Add(Tiemout, timeout);

        return services;
    }
}