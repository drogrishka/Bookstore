using Microsoft.AspNetCore.Diagnostics;

namespace Bookstore.Api.Infrastructure;

public sealed class ApiExceptionHandler(
    ILogger<ApiExceptionHandler> logger,
    IHostEnvironment environment) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title, detail) = exception switch
        {
            ArgumentException => (
                StatusCodes.Status400BadRequest,
                "Invalid request",
                exception.Message),

            InvalidOperationException => (
                StatusCodes.Status409Conflict,
                "Request conflict",
                exception.Message),

            _ => (
                StatusCodes.Status500InternalServerError,
                "Unexpected server error",
                environment.IsDevelopment()
                    ? exception.Message
                    : "An unexpected error occurred.")
        };

        if (statusCode >= 500)
        {
            logger.LogError(exception, "Unhandled exception.");
        }
        else
        {
            logger.LogWarning(exception, "Request failed with status {StatusCode}.", statusCode);
        }

        httpContext.Response.StatusCode = statusCode;

        await Results.Problem(
                statusCode: statusCode,
                title: title,
                detail: detail)
            .ExecuteAsync(httpContext);

        return true;
    }
}
