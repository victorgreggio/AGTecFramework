using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using AwesomeAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using AGTec.Analyzers;

namespace AGTec.Analyzers.Test;

public class EntityPublicSetterAnalyzerTests
{
    // A minimal stand-in for the AGTec.Domain.Entity<T> base the analyzer keys on.
    private const string EntityStub = """
        namespace AGTec.Domain
        {
            public abstract class Entity<T>
            {
                protected Entity(T id) { Id = id; }
                public T Id { get; }
            }
        }
        """;

    [Fact]
    public async Task Flags_a_public_setter_on_an_entity()
    {
        var source =
            EntityStub
            + """
                namespace Sample
                {
                    public class Forecast : AGTec.Domain.Entity<int>
                    {
                        public Forecast() : base(0) { }
                        public string Summary { get; set; } = "";
                    }
                }
                """;

        var diagnostics = await AnalyzeAsync(source);

        diagnostics.Should().ContainSingle(d => d.Id == EntityPublicSetterAnalyzer.DiagnosticId);
        diagnostics
            .Single(d => d.Id == EntityPublicSetterAnalyzer.DiagnosticId)
            .GetMessage(CultureInfo.InvariantCulture)
            .Should()
            .Contain("Summary");
    }

    [Fact]
    public async Task Allows_private_setters_init_and_get_only()
    {
        var source =
            EntityStub
            + """
                namespace Sample
                {
                    public class Forecast : AGTec.Domain.Entity<int>
                    {
                        public Forecast() : base(0) { }
                        public string Summary { get; private set; } = "";
                        public int Celsius { get; init; }
                        public string Label { get; } = "";
                    }
                }
                """;

        var diagnostics = await AnalyzeAsync(source);

        diagnostics.Should().NotContain(d => d.Id == EntityPublicSetterAnalyzer.DiagnosticId);
    }

    [Fact]
    public async Task Ignores_public_setters_on_non_entities()
    {
        var source =
            EntityStub
            + """
                namespace Sample
                {
                    public class PlainDto
                    {
                        public string Summary { get; set; } = "";
                    }
                }
                """;

        var diagnostics = await AnalyzeAsync(source);

        diagnostics.Should().NotContain(d => d.Id == EntityPublicSetterAnalyzer.DiagnosticId);
    }

    private static async Task<ImmutableArray<Diagnostic>> AnalyzeAsync(string source)
    {
        var references = AppDomain
            .CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
            .Select(a => (MetadataReference)MetadataReference.CreateFromFile(a.Location))
            .ToList();

        var compilation = CSharpCompilation.Create(
            "AnalyzerTest",
            [CSharpSyntaxTree.ParseText(source)],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );

        // The snippet must itself compile, or the test is meaningless.
        compilation.GetDiagnostics().Should().NotContain(d => d.Severity == DiagnosticSeverity.Error);

        var withAnalyzers = compilation.WithAnalyzers(
            ImmutableArray.Create<DiagnosticAnalyzer>(new EntityPublicSetterAnalyzer())
        );

        return await withAnalyzers.GetAnalyzerDiagnosticsAsync();
    }
}
