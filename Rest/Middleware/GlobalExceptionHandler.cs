using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Rest.Middleware;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IHostEnvironment env) 
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title, details) = MapException(exception);

        logger.LogError(exception, "Exception occurred: {Message}", exception.Message);

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = details,
            Instance = $"{context.Request.Method} {context.Request.Path}",
            Type = exception.GetType().Name
        };

        problemDetails.Extensions.Add("traceId", context.TraceIdentifier);

        if (env.IsDevelopment())
        {
            problemDetails.Extensions.Add("stackTrace", exception.StackTrace);
        }

        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }

    private static (int StatusCode, string Title, string Details) MapException(Exception exception)
    {
        return exception switch
        {
            ArgumentException ex => (StatusCodes.Status400BadRequest, "Invalid Argument", ex.Message),
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Unauthorized", "Access Denied"),
            KeyNotFoundException => (StatusCodes.Status404NotFound, "Not Found", "Resource not found"),

            _ => (StatusCodes.Status500InternalServerError, "Internal Server Error", "An unexpected error occurred.")
        };
    }
}