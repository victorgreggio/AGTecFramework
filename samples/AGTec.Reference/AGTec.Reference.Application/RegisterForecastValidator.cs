using AGTec.Reference.Domain;
using FluentValidation;

namespace AGTec.Reference.Application;

/// <summary>
/// Validates the command before it reaches the handler — run automatically by the
/// Wolverine FluentValidation middleware in the AGTec pipeline. Domain invariants still
/// live on the aggregate; this is the fast, user-facing input check.
/// </summary>
public sealed class RegisterForecastValidator : AbstractValidator<RegisterForecast>
{
    public RegisterForecastValidator()
    {
        RuleFor(x => x.Celsius).InclusiveBetween(Temperature.MinCelsius, Temperature.MaxCelsius);

        RuleFor(x => x.Summary).NotEmpty().MaximumLength(200);
    }
}
