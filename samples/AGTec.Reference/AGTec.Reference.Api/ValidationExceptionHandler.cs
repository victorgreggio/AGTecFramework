using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AGTec.Reference.Api;

/// <summary>
/// Translates validation failures into an RFC 9457 Problem Details 400 — both
/// FluentValidation failures (from the Wolverine pipeline) and domain/argument guards.
/// One handler for the whole app replaces per-action try/catch.
/// </summary>
internal sealed class ValidationExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken
    )
    {
        var detail = exception switch
        {
            FluentValidation.ValidationException fv => string.Join("; ", fv.Errors.Select(e => e.ErrorMessage)),
            ArgumentException arg => arg.Message,
            _ => null,
        };

        if (detail is null)
        {
            return false; // not a validation error — let the default handler deal with it
        }

        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;

        return await problemDetails.TryWriteAsync(
            new ProblemDetailsContext
            {
                HttpContext = httpContext,
                Exception = exception,
                ProblemDetails = new ProblemDetails
                {
                    Title = "Validation failed",
                    Detail = detail,
                    Status = StatusCodes.Status400BadRequest,
                },
            }
        );
    }
}
