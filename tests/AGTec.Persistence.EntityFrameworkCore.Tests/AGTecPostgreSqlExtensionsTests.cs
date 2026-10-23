using AGTec.Persistence.EntityFrameworkCore;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AGTec.Persistence.EntityFrameworkCore.Tests;

public sealed class AGTecPostgreSqlExtensionsTests
{
    private const string ConnectionString = "Host=localhost;Database=agtec_tests;Username=postgres;Password=postgres";

    [Fact]
    public void AddAGTecPostgreSqlDbContext_registers_a_postgresql_context()
    {
        var services = new ServiceCollection();

        services.AddAGTecPostgreSqlDbContext<TestContext>(ConnectionString);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TestContext>();

        context.Database.ProviderName.Should().Be("Npgsql.EntityFrameworkCore.PostgreSQL");
        context.Database.GetDbConnection().ConnectionString.Should().Be(ConnectionString);
    }

    [Fact]
    public void Configuration_overload_requires_the_named_connection_string()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        var act = () => services.AddAGTecPostgreSqlDbContext<TestContext>(configuration, "orders");

        act.Should().Throw<InvalidOperationException>().WithMessage("*ConnectionStrings:orders*");
    }

    [Fact]
    public void Rejects_an_invalid_connection_string()
    {
        var services = new ServiceCollection();

        var act = () => services.AddAGTecPostgreSqlDbContext<TestContext>("not a connection string");

        act.Should().Throw<ArgumentException>().WithMessage("*connection string is invalid*");
    }

    private sealed class TestContext(DbContextOptions<TestContext> options) : DbContext(options);
}
