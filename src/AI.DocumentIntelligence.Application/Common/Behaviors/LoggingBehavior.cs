using AI.DocumentIntelligence.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AI.DocumentIntelligence.Application.Common.Behaviors;

/// <summary>
/// MediatR pipeline behavior that logs the start and completion of every request, including whether
/// the resulting <see cref="Result"/> represented success or failure (and the failing error code).
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public sealed partial class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    /// <inheritdoc />
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        string requestName = typeof(TRequest).Name;
        LogHandling(logger, requestName);

        TResponse response = await next();

        if (response is Result { IsFailure: true } failed)
        {
            LogFailure(logger, requestName, failed.Error.Code, failed.Error.Description);
        }
        else
        {
            LogHandled(logger, requestName);
        }

        return response;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Handling {RequestName}")]
    private static partial void LogHandling(ILogger logger, string requestName);

    [LoggerMessage(Level = LogLevel.Warning, Message = "{RequestName} failed with {ErrorCode}: {ErrorDescription}")]
    private static partial void LogFailure(ILogger logger, string requestName, string errorCode, string errorDescription);

    [LoggerMessage(Level = LogLevel.Information, Message = "Handled {RequestName}")]
    private static partial void LogHandled(ILogger logger, string requestName);
}
