using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace VatDesk.Api.ErrorHandling;

/// <summary>
/// Catches anything a controller didn't handle explicitly and returns a generic ProblemDetails
/// response — full exception detail is logged server-side, never sent to the client.
/// </summary>
public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Unhandled exception processing {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

        // The parameterless-contentType overload of WriteAsJsonAsync sets its own
        // "application/json" content type regardless of what's assigned beforehand — the
        // contentType argument here is the only thing that actually makes this RFC 7807
        // ProblemDetails, not silently plain JSON (caught by a test that checks the header).
        await httpContext.Response.WriteAsJsonAsync(
            new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An unexpected error occurred.",
                Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
            },
            options: null,
            contentType: "application/problem+json",
            cancellationToken);

        return true;
    }
}
