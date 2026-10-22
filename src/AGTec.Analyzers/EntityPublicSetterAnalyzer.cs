using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AGTec.Analyzers;

/// <summary>
/// AGTEC0001 — flags public setters on properties of types deriving from
/// <c>AGTec.Domain.Entity&lt;T&gt;</c>. Aggregates and entities should encapsulate state and
/// mutate through intention-revealing methods, not public setters (see design §6.3).
/// Private/protected setters, <c>init</c> accessors, and get-only properties are allowed.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class EntityPublicSetterAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "AGTEC0001";

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "Entity properties should not have public setters",
        messageFormat: "Property '{0}' on entity '{1}' has a public setter; mutate through a method to protect invariants",
        category: "Design",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Types deriving from AGTec.Domain.Entity<T> should encapsulate state; avoid public setters."
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(AnalyzeProperty, SymbolKind.Property);
    }

    private static void AnalyzeProperty(SymbolAnalysisContext context)
    {
        var property = (IPropertySymbol)context.Symbol;

        if (property.IsStatic || property.SetMethod is not { } setter)
        {
            return;
        }

        // Allowed: non-public setters and init-only accessors.
        if (setter.DeclaredAccessibility != Accessibility.Public || setter.IsInitOnly)
        {
            return;
        }

        if (!DerivesFromEntity(property.ContainingType))
        {
            return;
        }

        var location = setter.Locations.FirstOrDefault() ?? property.Locations[0];
        context.ReportDiagnostic(Diagnostic.Create(Rule, location, property.Name, property.ContainingType.Name));
    }

    private static bool DerivesFromEntity(INamedTypeSymbol? type)
    {
        for (var current = type?.BaseType; current is not null; current = current.BaseType)
        {
            if (current.Name == "Entity" && current.ContainingNamespace?.ToDisplayString() == "AGTec.Domain")
            {
                return true;
            }
        }

        return false;
    }
}
