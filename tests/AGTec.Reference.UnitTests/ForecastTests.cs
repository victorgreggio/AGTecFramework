using AGTec.Reference.Domain;
using AwesomeAssertions;

namespace AGTec.Reference.UnitTests;

public class ForecastTests
{
    [Fact]
    public void Register_creates_a_forecast_and_raises_a_domain_event()
    {
        var forecast = Forecast.Register(new DateOnly(2026, 10, 20), celsius: 21, summary: "  Mild  ");

        forecast.Id.Should().NotBe(Guid.Empty);
        forecast.Temperature.Celsius.Should().Be(21);
        forecast.Summary.Should().Be("Mild"); // trimmed

        forecast
            .DomainEvents.Should()
            .ContainSingle()
            .Which.Should()
            .BeOfType<ForecastRegistered>()
            .Which.ForecastId.Should()
            .Be(forecast.Id);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Register_rejects_blank_summary(string summary)
    {
        var act = () => Forecast.Register(new DateOnly(2026, 10, 20), 21, summary);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ReviseSummary_updates_and_trims()
    {
        var forecast = Forecast.Register(new DateOnly(2026, 10, 20), 21, "Mild");

        forecast.ReviseSummary("  Breezy  ");

        forecast.Summary.Should().Be("Breezy");
    }

    [Fact]
    public void ClearDomainEvents_empties_the_list()
    {
        var forecast = Forecast.Register(new DateOnly(2026, 10, 20), 21, "Mild");

        forecast.ClearDomainEvents();

        forecast.DomainEvents.Should().BeEmpty();
    }
}

public class TemperatureTests
{
    [Fact]
    public void Fahrenheit_is_computed_from_celsius()
    {
        new Temperature(21).Fahrenheit.Should().Be(69);
        new Temperature(0).Fahrenheit.Should().Be(32);
    }

    [Theory]
    [InlineData(Temperature.MinCelsius - 1)]
    [InlineData(Temperature.MaxCelsius + 1)]
    public void Out_of_range_is_rejected(int celsius)
    {
        var act = () => new Temperature(celsius);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Value_objects_are_equal_by_value()
    {
        new Temperature(15).Should().Be(new Temperature(15));
    }
}
