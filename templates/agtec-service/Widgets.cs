using System.Collections.Concurrent;
using AGTec.Security;

namespace AGTecService;

/// <summary>
/// A starter vertical slice. Replace with your domain: a AGTec.Domain aggregate, a
/// AGTec.Application command/query + Wolverine handlers, and a persistence adapter.
/// </summary>
public sealed record Widget(Guid Id, string Name);

public sealed record CreateWidget(string Name);

internal static class WidgetEndpoints
{
    private static readonly ConcurrentDictionary<Guid, Widget> Store = new();

    public static IEndpointRouteBuilder MapWidgetEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/widgets").WithTags("Widgets").RequireScope("api");

        group.MapGet("/", () => Results.Ok(Store.Values));

        group.MapPost(
            "/",
            (CreateWidget request) =>
            {
                var widget = new Widget(Guid.CreateVersion7(), request.Name);
                Store[widget.Id] = widget;
                return Results.Created($"/widgets/{widget.Id}", widget);
            }
        );

        return app;
    }
}
