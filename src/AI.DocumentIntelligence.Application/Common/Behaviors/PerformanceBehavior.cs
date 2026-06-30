using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AI.DocumentIntelligence.Application.Common.Behaviors;

/// <summary>
/// MediatR pipeline behavior that times each request and logs a warning when it exceeds
/// <see cref="LongRunningThresholdMilliseconds"/>, surfacing slow handlers for investigation.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public sealed partial class PerformanceBehavior<TRequest, TResponse>(
    ILogger<PerformanceBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    /// <summary>Requests slower than this (in milliseconds) are logged as long-running.</summary>
    private const long LongRunningThresholdMilliseconds = 500;

    /// <inheritdoc />
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        long startTimestamp = Stopwatch.GetTimestamp();

        TResponse response = await next();

        TimeSpan elapsed = Stopwatch.GetElapsedTime(startTimestamp);
        if (elapsed.TotalMilliseconds > LongRunningThresholdMilliseconds)
        {
            LogLongRunning(logger, typeof(TRequest).Name, (long)elapsed.TotalMilliseconds);
        }

        return response;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Long-running request {RequestName} took {ElapsedMilliseconds} ms")]
    private static partial void LogLongRunning(ILogger logger, string requestName, long elapsedMilliseconds);
}
