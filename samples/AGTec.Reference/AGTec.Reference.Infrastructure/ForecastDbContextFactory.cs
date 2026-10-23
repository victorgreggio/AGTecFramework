using AGTec.Persistence.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AGTec.Reference.Infrastructure;

public sealed class ForecastDbContextFactory : IDesignTimeDbContextFactory<ForecastDbContext>
{
    public ForecastDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__forecasts")
            ?? "Host=localhost;Port=5432;Database=forecasts;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<ForecastDbContext>();
        options.UseAGTecPostgreSql(connectionString);

        return new ForecastDbContext(options.Options);
    }
}
