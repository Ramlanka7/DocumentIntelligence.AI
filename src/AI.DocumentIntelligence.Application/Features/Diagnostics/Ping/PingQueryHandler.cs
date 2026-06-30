using AI.DocumentIntelligence.Application.Common.Messaging;
using AI.DocumentIntelligence.Domain.Common;

namespace AI.DocumentIntelligence.Application.Features.Diagnostics.Ping;

/// <summary>Handles <see cref="PingQuery"/> by echoing the message back inside a successful result.</summary>
public sealed class PingQueryHandler : IQueryHandler<PingQuery, PingResponse>
{
    /// <inheritdoc />
    public Task<Result<PingResponse>> Handle(PingQuery request, CancellationToken cancellationToken)
    {
        var response = new PingResponse($"Pong: {request.Message}", DateTimeOffset.UtcNow);
        return Task.FromResult(Result.Success(response));
    }
}
