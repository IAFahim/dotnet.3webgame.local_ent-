using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Rest.Exceptions;

namespace Rest.Middleware;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly IHostEnvironment _env;
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IHostEnvironment env)
    {
        _logger = logger;
        _env = env;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException)
        {
            _logger.LogInformation("Request was cancelled");
            return true;
        }

        _logger.LogError(
            exception,
            "An unhandled exception occurred. TraceId: {TraceId}, Message: {Message}",
            context.TraceIdentifier,
            exception.Message);

        var (statusCode, title, detail) = MapException(exception);

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Type = exception.GetType().Name,
            Instance = $"{context.Request.Method} {context.Request.Path}"
        };

        problemDetails.Extensions.Add("traceId", context.TraceIdentifier);

        if (_env.IsDevelopment()) problemDetails.Extensions.Add("stackTrace", exception.StackTrace);

        if (exception is ValidationException validationEx) problemDetails.Extensions.Add("errors", validationEx.Errors);

        context.Response.StatusCode = statusCode;

        await context.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }

    private static (int StatusCode, string Title, string Detail) MapException(Exception exception)
    {
        return exception switch
        {
            NotFoundException ex => (StatusCodes.Status404NotFound, "Resource Not Found", ex.Message),
            ValidationException ex => (StatusCodes.Status400BadRequest, "Validation Error", ex.Message),
            UnauthorizedException ex => (StatusCodes.Status401Unauthorized, "Unauthorized", ex.Message),
            KeyNotFoundException => (StatusCodes.Status404NotFound, "Key Not Found",
                "The requested resource key was not found."),
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Access Denied",
                "You do not have permission to access this resource."),
            ArgumentException ex => (StatusCodes.Status400BadRequest, "Invalid Argument", ex.Message),
            _ => (StatusCodes.Status500InternalServerError, "Internal Server Error", "An unexpected error occurred.")
        };
    }
}