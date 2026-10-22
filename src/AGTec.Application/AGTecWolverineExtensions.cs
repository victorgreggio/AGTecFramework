using Wolverine;
using Wolverine.FluentValidation;

namespace AGTec.Application;

/// <summary>
/// Applies AGTecFramework's standard Wolverine pipeline: structured request logging and
/// FluentValidation on every command/query. Marten transactions + the outbox are added
/// by the persistence layer (so this stays persistence-agnostic).
/// </summary>
public static class AGTecWolverineExtensions
{
    public static WolverineOptions ApplyAGTecDefaults(this WolverineOptions options)
    {
        options.UseFluentValidation();
        options.Policies.AddMiddleware(typeof(RequestLoggingMiddleware));

        return options;
    }
}
