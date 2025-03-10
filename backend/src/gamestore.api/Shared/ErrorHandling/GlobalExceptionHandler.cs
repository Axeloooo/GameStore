using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;

namespace gamestore.api.Shared.ErrorHandling;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken
    )
    {
        ActivityTraceId? traceId = Activity.Current?.TraceId;

        logger.LogError(
            exception,
            "Could not process the request on machine {Machine}. Trace ID: {TraceId}",
            Environment.MachineName,
            traceId
        );

        await Results
            .Problem(
                title: "An error occurred while processing your request.",
                statusCode: StatusCodes.Status500InternalServerError,
                extensions: new Dictionary<string, object?> { { "traceId", traceId.ToString() } }
            )
            .ExecuteAsync(httpContext);

        throw new NotImplementedException();
    }
}
