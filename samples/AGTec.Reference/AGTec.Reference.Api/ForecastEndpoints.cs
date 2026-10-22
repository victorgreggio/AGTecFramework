using System.Text;
using Microsoft.Extensions.DependencyInjection;
using AGTec.Platform;
using AGTec.Reference.Application;
using AGTec.Reference.Grpc.Contracts;
using AGTec.Security;
using Wolverine;

namespace AGTec.Reference.Api;

/// <summary>Free-text note attached to a forecast, stored in the object store.</summary>
public sealed record NoteRequest(string Text);

/// <summary>
/// Minimal API group. Each endpoint is a thin shell: bind, send the message to the
/// Wolverine bus, shape the response. No business logic, no persistence here.
/// </summary>
internal static class ForecastEndpoints
{
    public static IEndpointRouteBuilder MapForecastEndpoints(this IEndpointRouteBuilder app)
    {
        // All forecast endpoints require a token carrying the "api" scope.
        var group = app.MapGroup("/forecasts").WithTags("Forecasts").RequireScope("api");

        group
            .MapPost(
                "/",
                async (RegisterForecastRequest request, IMessageBus bus, CancellationToken cancellationToken) =>
                {
                    var id = await bus.InvokeAsync<Guid>(
                        new RegisterForecast(request.Date, request.Celsius, request.Summary),
                        cancellationToken
                    );

                    return Results.Created($"/forecasts/{id}", new { id });
                }
            )
            .WithName("RegisterForecast");

        group
            .MapGet(
                "/{id:guid}",
                async (Guid id, IMessageBus bus, CancellationToken cancellationToken) =>
                {
                    var forecast = await bus.InvokeAsync<ForecastDto?>(new GetForecast(id), cancellationToken);
                    return forecast is null ? Results.NotFound() : Results.Ok(forecast);
                }
            )
            .WithName("GetForecast");

        // Full-text search over the search read model (Elasticsearch in production).
        group
            .MapGet(
                "/search",
                async (string term, IMessageBus bus, CancellationToken cancellationToken) =>
                {
                    var results = await bus.InvokeAsync<IReadOnlyList<ForecastDto>>(
                        new SearchForecasts(term),
                        cancellationToken
                    );
                    return Results.Ok(results);
                }
            )
            .WithName("SearchForecasts");

        // Attach / read a free-text note, stored via the object store (Azure Blob in prod).
        const string notesContainer = "forecast-notes";

        group
            .MapPut(
                "/{id:guid}/note",
                async (Guid id, NoteRequest request, IObjectStore store, CancellationToken cancellationToken) =>
                {
                    using var content = new MemoryStream(Encoding.UTF8.GetBytes(request.Text));
                    await store.PutAsync(notesContainer, id.ToString(), content, "text/plain", cancellationToken);
                    return Results.NoContent();
                }
            )
            .WithName("SetForecastNote");

        group
            .MapGet(
                "/{id:guid}/note",
                async (Guid id, IObjectStore store, CancellationToken cancellationToken) =>
                {
                    await using var stream = await store.GetAsync(notesContainer, id.ToString(), cancellationToken);
                    if (stream is null)
                    {
                        return Results.NotFound();
                    }

                    using var reader = new StreamReader(stream);
                    return Results.Ok(new { note = await reader.ReadToEndAsync(cancellationToken) });
                }
            )
            .WithName("GetForecastNote");

        // Hand the client a short-lived SAS URL so it uploads the note directly to storage,
        // bypassing the app. Available only when the store supports pre-signed URLs (AzureBlob).
        group
            .MapPost(
                "/{id:guid}/note/upload-url",
                async (Guid id, IServiceProvider services, CancellationToken cancellationToken) =>
                {
                    var urls = services.GetService<IObjectUrlProvider>();
                    if (urls is null)
                    {
                        return Results.Problem(
                            "Direct uploads require a storage provider that supports pre-signed URLs "
                                + "(set AGTec:Storage:Provider=AzureBlob).",
                            statusCode: StatusCodes.Status501NotImplemented
                        );
                    }

                    var uploadUrl = await urls.CreateUploadUrlAsync(
                        notesContainer,
                        id.ToString(),
                        TimeSpan.FromMinutes(15),
                        "text/plain",
                        cancellationToken
                    );
                    return Results.Ok(new { uploadUrl });
                }
            )
            .WithName("GetForecastNoteUploadUrl");

        // Internal synchronous gRPC call: enrich the forecast with advice from the Advisory service.
        group
            .MapGet(
                "/{id:guid}/advisory",
                async (
                    Guid id,
                    IMessageBus bus,
                    AdvisoryService.AdvisoryServiceClient advisory,
                    CancellationToken cancellationToken
                ) =>
                {
                    var forecast = await bus.InvokeAsync<ForecastDto?>(new GetForecast(id), cancellationToken);
                    if (forecast is null)
                    {
                        return Results.NotFound();
                    }

                    var reply = await advisory.GetAdvisoryAsync(
                        new AdvisoryRequest { Celsius = forecast.Celsius },
                        cancellationToken: cancellationToken
                    );

                    return Results.Ok(new { forecast, advice = reply.Advice });
                }
            )
            .WithName("GetForecastAdvisory");

        return app;
    }
}
