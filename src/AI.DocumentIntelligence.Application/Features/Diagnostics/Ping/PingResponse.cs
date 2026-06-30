namespace AI.DocumentIntelligence.Application.Features.Diagnostics.Ping;

/// <summary>The payload returned by <see cref="PingQuery"/>.</summary>
/// <param name="Reply">An echo of the supplied message.</param>
/// <param name="RespondedAt">When the handler produced the response.</param>
public sealed record PingResponse(string Reply, DateTimeOffset RespondedAt);
