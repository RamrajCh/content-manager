using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace ContentManager.Api.Infrastructure;

public sealed class SafeExceptionHandler(
    IProblemDetailsService problems, ILogger<SafeExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        logger.LogError("Unhandled request failure Type={ExceptionType} TraceId={TraceId}",
            exception.GetType().Name, System.Diagnostics.Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier);
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        return await problems.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An unexpected error occurred.",
                Extensions = { ["code"] = "UNEXPECTED_ERROR" }
            }
        });
    }
}
