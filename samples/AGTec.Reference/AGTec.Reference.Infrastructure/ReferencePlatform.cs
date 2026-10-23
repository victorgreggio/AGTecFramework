using System.Globalization;
using AGTec.Application;
using AGTec.Persistence.EntityFrameworkCore;
using AGTec.Platform;
using AGTec.Platform.Azure;
using AGTec.Platform.ClamAv;
using AGTec.Platform.Dapr;
using AGTec.Reference.Application;
using Elastic.Clients.Elasticsearch;
using FluentValidation;
using JasperFx.CodeGeneration.Model;
using Marten;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.Marten;
using Wolverine.Postgresql;

namespace AGTec.Reference.Infrastructure;

/// <summary>
/// Shared persistence + messaging wiring for the reference service. One method configures
/// EF Core/PostgreSQL (relational write model) and Wolverine (mediator + pipeline +
/// transactional outbox), while Marten is retained for the document projection. The
/// transport is chosen from configuration (<c>AGTec:Messaging:Provider</c>), so the API,
/// Worker, and integration tests reuse the exact same core and the broker is swapped by
/// config, not code (§8 portability).
/// </summary>
public static class ReferencePlatform
{
    public const string ConnectionStringName = "forecasts";

    public static IHostApplicationBuilder AddReferencePlatform(
        this IHostApplicationBuilder builder,
        MessagingRole roles,
        Action<WolverineOptions>? configure = null
    )
    {
        var connectionString =
            builder.Configuration.GetConnectionString(ConnectionStringName)
            ?? "Host=localhost;Port=5432;Database=forecasts;Username=postgres;Password=postgres";

        builder.Services.AddDbContextWithWolverineIntegration<ForecastDbContext>(
            options => options.UseAGTecPostgreSql(connectionString),
            connectionString
        );

        builder.Services.AddMarten(options =>
        {
            options.Connection(connectionString);
            options.DatabaseSchemaName = "forecasts";
        });

        // Ports → EF Core relational adapters.
        builder.Services.AddScoped<IForecastRepository, EfCoreForecastRepository>();
        builder.Services.AddScoped<IForecastReadStore, EfCoreForecastReadStore>();

        // Search read store, provider selected by config (InMemory default | Elasticsearch).
        AddForecastSearch(builder);

        // Cloud-coupled ports (secrets, object storage), provider selected by config.
        AddPlatformServices(builder);

        // Validators consumed by the Wolverine FluentValidation middleware.
        builder.Services.AddScoped<IValidator<RegisterForecast>, RegisterForecastValidator>();

        builder.UseWolverine(options =>
        {
            options.ApplyAGTecDefaults();
            options.ServiceLocationPolicy = ServiceLocationPolicy.AllowedButWarn;

            // Commit the EF Core aggregate and the PostgreSQL outbox in one transaction.
            options.UseEntityFrameworkCoreTransactions();
            options.Policies.AutoApplyTransactions();
            options.UseEntityFrameworkCoreWolverineManagedMigrations();
            options.PersistMessagesWithPostgresql(connectionString, "wolverine").Enroll<ForecastDbContext>();

            // Read handlers live in Application; write handlers that need EF transactions live in Infrastructure.
            options.Discovery.IncludeAssembly(typeof(RegisterForecast).Assembly);
            options.Discovery.IncludeAssembly(typeof(ForecastDbContext).Assembly);

            // Transport chosen by configuration (Local / RabbitMq / AzureServiceBus).
            ForecastMessaging.Configure(options, builder.Configuration, roles);

            configure?.Invoke(options);
        });

        return builder;
    }

    private static void AddForecastSearch(IHostApplicationBuilder builder)
    {
        // AGTec:Search:Provider = InMemory (default) | Elasticsearch
        var provider = builder.Configuration["AGTec:Search:Provider"] ?? "InMemory";

        if (provider.Equals("Elasticsearch", StringComparison.OrdinalIgnoreCase))
        {
            var url = builder.Configuration.GetConnectionString("search") ?? "http://localhost:9200";
            builder.Services.AddSingleton(ElasticsearchForecastSearchStore.CreateClient(url));
            builder.Services.AddSingleton<IForecastSearchStore, ElasticsearchForecastSearchStore>();
        }
        else
        {
            builder.Services.AddSingleton<IForecastSearchStore, InMemoryForecastSearchStore>();
        }
    }

    private static void AddPlatformServices(IHostApplicationBuilder builder)
    {
        // Secrets: Configuration (default) | AzureKeyVault | Dapr
        var secretsProvider = builder.Configuration["AGTec:Secrets:Provider"] ?? "Configuration";
        if (secretsProvider.Equals("AzureKeyVault", StringComparison.OrdinalIgnoreCase))
        {
            builder.Services.AddAzureKeyVaultSecrets(new Uri(builder.Configuration["AGTec:Secrets:VaultUri"]!));
        }
        else if (secretsProvider.Equals("Dapr", StringComparison.OrdinalIgnoreCase))
        {
            // Dapr plugs in behind the same ISecretStore port; the actual backend is chosen
            // in the Dapr component YAML, not here (spike — see docs/adr/ADR-001).
            builder.Services.AddDaprSecrets(builder.Configuration["AGTec:Secrets:DaprStoreName"] ?? "secretstore");
        }
        else
        {
            builder.Services.AddConfigurationSecrets();
        }

        // Object storage: InMemory (default) | FileSystem | AzureBlob
        var storageProvider = builder.Configuration["AGTec:Storage:Provider"] ?? "InMemory";
        if (storageProvider.Equals("AzureBlob", StringComparison.OrdinalIgnoreCase))
        {
            var connectionString = builder.Configuration.GetConnectionString("blobs") ?? "UseDevelopmentStorage=true";
            builder.Services.AddAzureBlobStorage(connectionString);
        }
        else if (storageProvider.Equals("FileSystem", StringComparison.OrdinalIgnoreCase))
        {
            var root =
                builder.Configuration["AGTec:Storage:FileSystem:Root"]
                ?? Path.Combine(AppContext.BaseDirectory, "local-data", "blobs");
            builder.Services.AddFileSystemStorage(root);
        }
        else
        {
            builder.Services.AddInMemoryStorage();
        }

        // Malware scanning: None (default — scan at the storage layer) | ClamAv.
        // The scanner decorates whichever object store was registered above.
        var scanner = builder.Configuration["AGTec:Storage:Scanner"] ?? "None";
        if (scanner.Equals("ClamAv", StringComparison.OrdinalIgnoreCase))
        {
            var endpoint = (builder.Configuration.GetConnectionString("clamav") ?? "clamav:3310").Split(':');
            var host = endpoint[0];
            var port = endpoint.Length > 1 ? int.Parse(endpoint[1], CultureInfo.InvariantCulture) : 3310;
            builder.Services.AddClamAvScanner(host, port);
        }
        else
        {
            builder.Services.AddNoOpMalwareScanner();
        }

        builder.Services.ScanObjectStoreForMalware();
    }
}
