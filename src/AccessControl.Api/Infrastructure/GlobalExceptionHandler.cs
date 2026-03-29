using AccessControl.Domain.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AccessControl.Api.Infrastructure;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title, detail) = MapException(exception);

        if (statusCode >= 500)
        {
            logger.LogError(exception, "An error occurred while processing the request: {Message}", exception.Message);
        }
        else
        {
            logger.LogWarning(exception, "Request failed ({StatusCode}): {Message}", statusCode, exception.Message);
        }

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path,
            Type = $"https://httpstatuses.com/{statusCode}"
        };

        problemDetails.Extensions.Add("traceId", httpContext.TraceIdentifier);

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/problem+json";

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }

    /// <summary>
    /// Maps the thrown exception to the appropriate HTTP status and client messages.
    /// This is where you register your custom business exceptions.
    /// </summary>
    private static (int StatusCode, string Title, string Detail) MapException(Exception exception)
    {
        return exception switch
        {
            ValidationException validationEx => (
                StatusCodes.Status400BadRequest,
                "Validation Error",
                string.Join("; ", validationEx.Errors.Select(e => e.ErrorMessage))),

            DomainValidationException domainEx => (
                StatusCodes.Status400BadRequest,
                "Bad Request",
                domainEx.Message),

            UnauthorizedAccessException => (
                StatusCodes.Status401Unauthorized,
                "Unauthorized",
                "Authentication is required to access this resource."),

            KeyNotFoundException => (
                StatusCodes.Status404NotFound,
                "Not Found",
                "The requested resource was not found."),

            BusinessRuleException businessEx => (
                StatusCodes.Status409Conflict,
                "Conflict",
                businessEx.Message),

            DbUpdateException => (
                StatusCodes.Status409Conflict,
                "Conflict",
                "The operation failed due to a data conflict. A resource with the same unique value may already exist."),

            InvalidOperationException => (
                StatusCodes.Status500InternalServerError,
                "Internal Server Error",
                "An unexpected error occurred."),

            _ => (
                StatusCodes.Status500InternalServerError,
                "Internal Server Error",
                "An unexpected error occurred.")
        };
    }
}
