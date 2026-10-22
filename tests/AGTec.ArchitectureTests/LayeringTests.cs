using AwesomeAssertions;
using NetArchTest.Rules;

namespace AGTec.ArchitectureTests;

/// <summary>
/// Enforces the layering the design asserts: the domain/application core stays free of
/// web, persistence, and cloud SDKs, and cloud SDKs appear only in adapter packages.
/// These run in CI so a violation fails the build instead of eroding silently.
/// </summary>
public class LayeringTests
{
    [Fact]
    public void Framework_domain_has_no_infrastructure_dependencies()
    {
        var result = Types
            .InAssembly(typeof(AGTec.Domain.IDomainEvent).Assembly)
            .Should()
            .NotHaveDependencyOnAny(
                "Microsoft.AspNetCore",
                "Microsoft.EntityFrameworkCore",
                "Marten",
                "Wolverine",
                "Azure"
            )
            .GetResult();

        result.IsSuccessful.Should().BeTrue(Offenders(result));
    }

    [Fact]
    public void Framework_application_stays_free_of_web_persistence_and_cloud()
    {
        var result = Types
            .InAssembly(typeof(AGTec.Application.ICommand).Assembly)
            .Should()
            .NotHaveDependencyOnAny("Microsoft.AspNetCore", "Microsoft.EntityFrameworkCore", "Marten", "Azure")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(Offenders(result));
    }

    [Fact]
    public void Platform_abstractions_have_no_cloud_sdk_dependency()
    {
        var result = Types
            .InAssembly(typeof(AGTec.Platform.ISecretStore).Assembly)
            .Should()
            .NotHaveDependencyOnAny("Azure", "Amazon", "Microsoft.AspNetCore")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(Offenders(result));
    }

    [Fact]
    public void Reference_domain_depends_on_nothing_outward()
    {
        var result = Types
            .InAssembly(typeof(AGTec.Reference.Domain.Forecast).Assembly)
            .Should()
            .NotHaveDependencyOnAny(
                "AGTec.Reference.Application",
                "AGTec.Reference.Infrastructure",
                "AGTec.Reference.Api",
                "Marten",
                "Wolverine",
                "Microsoft.AspNetCore",
                "Microsoft.EntityFrameworkCore",
                "Azure",
                "Elastic"
            )
            .GetResult();

        result.IsSuccessful.Should().BeTrue(Offenders(result));
    }

    [Fact]
    public void Reference_application_does_not_depend_on_infrastructure_or_sdks()
    {
        var result = Types
            .InAssembly(typeof(AGTec.Reference.Application.RegisterForecast).Assembly)
            .Should()
            .NotHaveDependencyOnAny(
                "AGTec.Reference.Infrastructure",
                "AGTec.Reference.Api",
                "Marten",
                "Microsoft.AspNetCore",
                "Azure",
                "Elastic"
            )
            .GetResult();

        result.IsSuccessful.Should().BeTrue(Offenders(result));
    }

    [Fact]
    public void Sanity_detector_sees_that_infrastructure_uses_marten()
    {
        // Guards against false positives: if the analyzer saw no dependencies at all, the
        // rules above would pass vacuously. Infrastructure genuinely depends on Marten.
        var martenConsumers = Types
            .InAssembly(typeof(AGTec.Reference.Infrastructure.ReferencePlatform).Assembly)
            .That()
            .HaveDependencyOn("Marten")
            .GetTypes();

        martenConsumers.Should().NotBeEmpty();
    }

    private static string Offenders(TestResult result) =>
        "offending types: " + string.Join(", ", result.FailingTypeNames ?? []);
}
