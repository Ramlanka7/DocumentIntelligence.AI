using AI.DocumentIntelligence.Application.Abstractions;
using AI.DocumentIntelligence.Application.Abstractions.Persistence;
using Microsoft.Extensions.Logging;

namespace AI.DocumentIntelligence.Api.Middleware;

/// <summary>
/// Writes HTTP-level audit records for security-sensitive paths after the response is sent.
/// Runs in a dedicated DI scope so it never shares a Unit of Work with the request handler,
/// avoiding double-SaveChanges or a disposed DbContext. Path matching is version-agnostic.
/// </summary>
public sealed class AuditMiddleware(
    RequestDelegate next,
    IServiceScopeFactory scopeFactory,
    ILogger<AuditMiddleware> logger)
{
    private static readonly Action<ILogger, string, Exception?> LogAuditFailure =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(1, nameof(AuditMiddleware)),
            "AuditMiddleware failed to record an audit entry for {Path}");

    public async Task InvokeAsync(HttpContext context)
    {
        await next(context);

        var action = DetermineAction(context);
        if (action is null)
        {
            return;
        }

        try
        {
            // Use a fresh scope so this audit write is independent of the request's UoW.
            await using var scope = scopeFactory.CreateAsyncScope();
            var auditService = scope.ServiceProvider.GetRequiredService<IAuditService>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            await auditService.LogAsync(
                action: action,
                entityType: "HttpRequest",
                details: $"{context.Request.Method} {context.Request.Path} → {context.Response.StatusCode}",
                ct: CancellationToken.None);

            await unitOfWork.SaveChangesAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            // Audit failures must not break the response already sent.
            LogAuditFailure(logger, context.Request.Path, ex);
        }
    }

    private static string? DetermineAction(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        var method = context.Request.Method;

        // Version-agnostic: match on path segments, not on /api/vN/ prefix.
        if (path.Contains("/auth/login", StringComparison.OrdinalIgnoreCase))
        {
            return "Http.Auth.Login";
        }

        if (path.Contains("/auth/logout", StringComparison.OrdinalIgnoreCase))
        {
            return "Http.Auth.Logout";
        }

        if (path.Contains("/auth/refresh", StringComparison.OrdinalIgnoreCase))
        {
            return "Http.Auth.Refresh";
        }

        if (path.Contains("/auth/register", StringComparison.OrdinalIgnoreCase))
        {
            return "Http.Auth.Register";
        }

        if (path.Contains("/documents", StringComparison.OrdinalIgnoreCase)
            && method.Equals("POST", StringComparison.OrdinalIgnoreCase))
        {
            return "Http.Document.Upload";
        }

        if (path.Contains("/documents", StringComparison.OrdinalIgnoreCase)
            && method.Equals("DELETE", StringComparison.OrdinalIgnoreCase))
        {
            return "Http.Document.Delete";
        }

        return null;
    }
}
