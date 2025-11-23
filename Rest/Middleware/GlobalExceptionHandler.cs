using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Rest.Exceptions;

namespace Rest.Middleware;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken token)
    {
        logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);

        var (statusCode, title, details, extensions) = MapException(exception);

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = details,
            Type = exception.GetType().Name,
            Instance = context.Request.Path
        };

        if (extensions is not null)
        {
            problemDetails.Extensions.Add("errors", extensions);
        }

        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsJsonAsync(problemDetails, token);
        return true;
    }

    private static (int Status, string Title, string Detail, object? Extensions) MapException(Exception ex)
    {
        return ex switch
        {
            ValidationException valEx => (
                StatusCodes.Status400BadRequest,
                "Validation Failure",
                "One or more validation errors occurred", valEx.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())
            ),
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Unauthorized", "Access Denied", null),
            _ => (StatusCodes.Status500InternalServerError, "Server Error", "An unexpected error occurred", null)
        };
    }
}
