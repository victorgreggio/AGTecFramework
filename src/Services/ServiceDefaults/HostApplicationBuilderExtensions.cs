using AGTec.Common.BackgroundTaskQueue;
using AGTec.Services.ServiceDefaults.Cors;
using AGTec.Services.ServiceDefaults.Filters;
using AGTec.Services.ServiceDefaults.Swagger;
using Asp.Versioning;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
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
        // Authentication
        builder.AddJwtBearerAuthentication();

        // OpenTelemetry
        builder.ConfigureOpenTelemetry();

        // Service Discovery
        builder.Services.AddServiceDiscovery();

        // Http Resilience
        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            http.AddStandardResilienceHandler();
            http.AddServiceDiscovery();
        });

        // Queued Tasks
        builder.Services.AddHostedService<QueuedHostedService>();
        builder.Services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();

        // Controllers with Action Filters
        builder.Services.AddControllersWithViews(opts =>
        {
            opts.Filters.Add<InvalidModelStateFilter>();
        });

        // API Versioning
        builder.AddApiVersioning();

        // Swagger
        builder.AddSwagger();

        // CORS
        builder.AddCorsPolicy();

        // OpenAPI
        builder.Services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer((document, context, cancellationToken) =>
            {
                document.Components ??= new Microsoft.OpenApi.Models.OpenApiComponents();
                document.Components.SecuritySchemes.Add("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    Description = "JWT Authorization header using the Bearer scheme. Enter your token in the text input below.",
                    In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                    Name = "Authorization"
                });
                
                document.SecurityRequirements.Add(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
                {
                    {
                        new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                        {
                            Reference = new Microsoft.OpenApi.Models.OpenApiReference
                            {
                                Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
                
                return Task.CompletedTask;
            });
        });

        return builder;
    }

    private static IHostApplicationBuilder AddApiVersioning(this IHostApplicationBuilder builder)
    {
        builder.Services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
            options.ApiVersionReader = ApiVersionReader.Combine(
                new UrlSegmentApiVersionReader(),
                new HeaderApiVersionReader("X-Api-Version"),
                new MediaTypeApiVersionReader("version"));
        }).AddApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });

        return builder;
    }

    private static IHostApplicationBuilder AddSwagger(this IHostApplicationBuilder builder)
    {
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();
        builder.Services.AddSwaggerGen(options =>
        {
            options.OperationFilter<SwaggerDefaultValues>();
            
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme. Enter your token in the text input below (do NOT include 'Bearer' prefix).",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    new string[] { }
                }
            });
        });

        return builder;
    }

    private static IHostApplicationBuilder AddCorsPolicy(this IHostApplicationBuilder builder)
    {
        var corsConfig = builder.Configuration.GetSection(CorsConfiguration.SectionName).Get<CorsConfiguration>() ?? new CorsConfiguration();

        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                if (corsConfig.AllowedOrigins?.Length > 0)
                {
                    policy.WithOrigins(corsConfig.AllowedOrigins);
                }
                else
                {
                    policy.AllowAnyOrigin();
                }

                if (corsConfig.AllowedMethods?.Length > 0)
                {
                    policy.WithMethods(corsConfig.AllowedMethods);
                }
                else
                {
                    policy.AllowAnyMethod();
                }

                if (corsConfig.AllowedHeaders?.Length > 0)
                {
                    policy.WithHeaders(corsConfig.AllowedHeaders);
                }
                else
                {
                    policy.AllowAnyHeader();
                }
            });
        });

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
            });
        
        builder.AddOpenTelemetryExporters();

        return builder;
    }

    private static IHostApplicationBuilder AddOpenTelemetryExporters(this IHostApplicationBuilder builder)
    {
        var useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);
        if (useOtlpExporter)
        {
            builder.Services.AddOpenTelemetry().UseOtlpExporter();
        }

        if (!string.IsNullOrEmpty(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
        {
            builder.Services.AddOpenTelemetry().UseAzureMonitor();
        }

        return builder;
    }

    private static IHealthChecksBuilder AddSelfCheck(this IHealthChecksBuilder builder) => builder.AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);
    private static IHealthChecksBuilder AddDbContextCheck<TContext>(this IHealthChecksBuilder builder) where TContext : DbContext
        => builder.AddDbContextCheck<TContext>("Database", HealthStatus.Unhealthy, ["ready"]);
}

