namespace AI.DocumentIntelligence.Application.Abstractions;

/// <summary>
/// Appends an audit record for a security- or business-relevant action. Implemented in the
/// Infrastructure layer; callers (handlers, middleware) interact only through this interface
/// so the Application layer stays free of persistence concerns.
/// </summary>
public interface IAuditService
{
    /// <summary>
    /// Records an audit event. The implementing service resolves the current user and IP address
    /// from the ambient context; callers only need to supply the action and entity details.
    /// </summary>
    /// <param name="action">A stable machine-readable code such as <c>"User.LoggedIn"</c>.</param>
    /// <param name="entityType">The type of entity affected, e.g. <c>"User"</c>.</param>
    /// <param name="entityId">The optional identifier of the affected entity.</param>
    /// <param name="details">Optional free-text supplementary information.</param>
    /// <param name="ct">Cancellation token.</param>
    public Task LogAsync(
        string action,
        string entityType,
        Guid? entityId = null,
        string? details = null,
        CancellationToken ct = default);
}
