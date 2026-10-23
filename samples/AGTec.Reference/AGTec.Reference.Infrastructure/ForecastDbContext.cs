using AGTec.Reference.Domain;
using Microsoft.EntityFrameworkCore;

namespace AGTec.Reference.Infrastructure;

/// <summary>
/// Relational write model for forecasts. Aggregate data is modeled as a normal PostgreSQL
/// table; Marten is reserved for the document projection and any future event-sourced model.
/// </summary>
public sealed class ForecastDbContext(DbContextOptions<ForecastDbContext> options) : DbContext(options)
{
    public DbSet<Forecast> Forecasts => Set<Forecast>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var forecast = modelBuilder.Entity<Forecast>();
        forecast.ToTable("forecasts", "reference");
        forecast.HasKey(x => x.Id);
        forecast.Property(x => x.Id).ValueGeneratedNever();
        forecast.Property(x => x.Date).IsRequired();
        forecast.Property(x => x.Summary).HasMaxLength(200).IsRequired();
        forecast.Property(x => x.Version).IsConcurrencyToken();
        forecast
            .Property(x => x.Temperature)
            .HasConversion(temperature => temperature.Celsius, celsius => new Temperature(celsius))
            .HasColumnName("celsius");
    }
}
