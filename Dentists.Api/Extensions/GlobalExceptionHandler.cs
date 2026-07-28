using Dentists.Application.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Dentists.Api.Extensions;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException || exception is TaskCanceledException)
        {
            _logger.LogWarning("The request to {Path} was aborted by the user.", httpContext.Request.Path);

            httpContext.Response.StatusCode = StatusCodes.Status499ClientClosedRequest;
            return true;
        }

        var problemDetails = CreateProblemDetails(httpContext, exception);

        httpContext.Response.StatusCode = problemDetails.Status!.Value;
        httpContext.Response.ContentType = "application/problem+json";

        // Serialize against the runtime type, otherwise the extra members of derived
        // types such as ValidationProblemDetails.Errors are dropped.
        await httpContext.Response.WriteAsJsonAsync(problemDetails, problemDetails.GetType(), cancellationToken);

        return true;
    }

    private ProblemDetails CreateProblemDetails(HttpContext httpContext, Exception exception)
    {
        switch (exception)
        {
            // Raised by the FluentValidation pipeline behavior before the handler runs.
            case ValidationException validationException:
                LogExpected(httpContext, validationException);

                var errors = validationException.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

                return new ValidationProblemDetails(errors)
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "One or more validation errors occurred.",
                    Instance = httpContext.Request.Path
                };

            case BusinessException businessException:
                LogExpected(httpContext, businessException);

                return new ProblemDetails
                {
                    Status = businessException.StatusCode,
                    Title = businessException.Title,
                    Detail = businessException.Message,
                    Instance = httpContext.Request.Path
                };

            // The document's etag moved on between loading it and saving it.
            case DbUpdateConcurrencyException concurrencyException:
                LogExpected(httpContext, concurrencyException);

                return new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = "The record was modified by another user",
                    Detail = "The record changed while this request was in flight. Reload it and try again.",
                    Instance = httpContext.Request.Path
                };

            // A rejection the model could not check up front, e.g. a unique key violation
            // or a document that grew past the Cosmos size limit.
            case DbUpdateException dbUpdateException:
                LogExpected(httpContext, dbUpdateException);

                return new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "The request could not be saved",
                    Detail = "The request violates a database constraint.",
                    Instance = httpContext.Request.Path
                };

            default:
                _logger.LogError(
                    exception,
                    "An unhandled error occurred while requesting {Path} with Method {Method}",
                    httpContext.Request.Path,
                    httpContext.Request.Method);

                return new ProblemDetails
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Title = "Internal Server Error",
                    Detail = "An unexpected internal system error occurred.",
                    Instance = httpContext.Request.Path
                };
        }
    }

    private void LogExpected(HttpContext httpContext, Exception exception)
    {
        _logger.LogWarning(
            exception,
            "A request to {Path} with Method {Method} was rejected: {Reason}",
            httpContext.Request.Path,
            httpContext.Request.Method,
            exception.Message);
    }
}
