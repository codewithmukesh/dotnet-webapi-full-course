using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Ticketing.Api.Common;

// Unexpected failures only. Expected ones ("sold out") are Results, not exceptions.
internal sealed partial class GlobalExceptionHandler(
    IProblemDetailsService problemDetails,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        // The client hung up. Not our bug, not an error log, nobody is waiting for a body.
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            LogRequestAborted(logger, httpContext.Request.Path.Value ?? "/");
            httpContext.Response.StatusCode = StatusCodes.Status499ClientClosedRequest;
            return true;
        }

        // Malformed JSON, wrong types, missing body: the CLIENT sent a bad request. That's a 400, not our 500.
        if (exception is BadHttpRequestException badRequest)
        {
            LogBadRequest(logger, httpContext.Request.Method, httpContext.Request.Path.Value ?? "/", badRequest.Message);
            httpContext.Response.StatusCode = badRequest.StatusCode;
            return await problemDetails.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                Exception = exception,
                ProblemDetails = new ProblemDetails
                {
                    Status = badRequest.StatusCode,
                    Title = "The request body is not valid.",
                    Detail = "Check the JSON shape and value types against the API reference.",
                },
            });
        }

        // Full details go to the logs, with the same traceId the client gets.
        LogUnhandled(logger, exception, httpContext.Request.Method, httpContext.Request.Path.Value ?? "/");

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Something went wrong on our side.",
                Detail = "Quote the traceId if you contact support.",
            },
        });
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Bad request for {Method} {Path}: {Reason}")]
    private static partial void LogBadRequest(ILogger logger, string method, string path, string reason);

    [LoggerMessage(Level = LogLevel.Error, Message = "Unhandled exception for {Method} {Path}")]
    private static partial void LogUnhandled(ILogger logger, Exception exception, string method, string path);

    [LoggerMessage(Level = LogLevel.Information, Message = "Request aborted by the client: {Path}")]
    private static partial void LogRequestAborted(ILogger logger, string path);
}
