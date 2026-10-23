using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace AGTec.Persistence.EntityFrameworkCore;

/// <summary>EF Core registration and configuration conventions for PostgreSQL.</summary>
public static class AGTecPostgreSqlExtensions
{
    /// <summary>
    /// Configures a DbContext options builder to use PostgreSQL. The connection string is
    /// validated when the host is composed, rather than failing later on the first request.
    /// </summary>
    public static DbContextOptionsBuilder UseAGTecPostgreSql(
        this DbContextOptionsBuilder options,
        string connectionString
    )
    {
        ArgumentNullException.ThrowIfNull(options);
        ValidateConnectionString(connectionString);
        return options.UseNpgsql(connectionString);
    }

    /// <summary>
    /// Registers a relational <typeparamref name="TContext"/> backed by PostgreSQL.
    /// Use this for aggregate and transactional data; document and event-sourced models
    /// remain a better fit for a document store such as Marten.
    /// </summary>
    public static IServiceCollection AddAGTecPostgreSqlDbContext<TContext>(
        this IServiceCollection services,
        string connectionString,
        Action<DbContextOptionsBuilder>? configure = null
    )
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(services);
        ValidateConnectionString(connectionString);

        services.AddDbContext<TContext>(options =>
        {
            options.UseAGTecPostgreSql(connectionString);
            configure?.Invoke(options);
        });

        return services;
    }

    /// <summary>
    /// Registers a relational <typeparamref name="TContext"/> using a named connection
    /// string. Missing or malformed configuration fails during host composition.
    /// </summary>
    public static IServiceCollection AddAGTecPostgreSqlDbContext<TContext>(
        this IServiceCollection services,
        IConfiguration configuration,
        string connectionStringName,
        Action<DbContextOptionsBuilder>? configure = null
    )
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionStringName);

        var connectionString = configuration.GetConnectionString(connectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"ConnectionStrings:{connectionStringName} is required for PostgreSQL persistence."
            );
        }

        return services.AddAGTecPostgreSqlDbContext<TContext>(connectionString, configure);
    }

    private static void ValidateConnectionString(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        try
        {
            _ = new NpgsqlConnectionStringBuilder(connectionString);
        }
        catch (ArgumentException exception)
        {
            throw new ArgumentException(
                "The PostgreSQL connection string is invalid.",
                nameof(connectionString),
                exception
            );
        }
    }
}
