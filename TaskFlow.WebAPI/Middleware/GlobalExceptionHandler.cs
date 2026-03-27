using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Application.Common.Exceptions;
using ValidationException = TaskFlow.Application.Common.Exceptions.ValidationException;

namespace TaskFlow.WebAPI.Middleware;

internal sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title) = exception switch
        {
            ValidationException      => (StatusCodes.Status400BadRequest,        "Validation Error"),
            NotFoundException        => (StatusCodes.Status404NotFound,           "Not Found"),
            ForbiddenAccessException => (StatusCodes.Status403Forbidden,          "Forbidden"),
            _                        => (StatusCodes.Status500InternalServerError, "Internal Server Error"),
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "Unhandled exception occurred");

        httpContext.Response.StatusCode = statusCode;

        if (exception is ValidationException validationEx)
        {
            var problem = new ValidationProblemDetails
            {
                Status = statusCode,
                Title  = title,
                Type   = $"https://httpstatuses.io/{statusCode}",
            };
            foreach (var (key, messages) in validationEx.Errors)
                problem.Errors[key] = messages;
            await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        }
        else
        {
            var problem = new ProblemDetails
            {
                Status = statusCode,
                Title  = title,
                Detail = exception.Message,
                Type   = $"https://httpstatuses.io/{statusCode}",
            };
            await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        }

        return true;
    }
}
