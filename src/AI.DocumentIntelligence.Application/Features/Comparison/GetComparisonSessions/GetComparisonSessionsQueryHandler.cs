using AI.DocumentIntelligence.Application.Abstractions.Identity;
using AI.DocumentIntelligence.Application.Abstractions.Persistence;
using AI.DocumentIntelligence.Application.Common.Messaging;
using AI.DocumentIntelligence.Domain.Common;
using AI.DocumentIntelligence.Domain.Entities;
using AI.DocumentIntelligence.Domain.Errors;

namespace AI.DocumentIntelligence.Application.Features.Comparison.GetComparisonSessions;

/// <summary>Returns the current user's comparison sessions, owner-scoped for security.</summary>
internal sealed class GetComparisonSessionsQueryHandler(
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser)
    : IQueryHandler<GetComparisonSessionsQuery, IReadOnlyList<ComparisonSessionSummaryDto>>
{
    public async Task<Result<IReadOnlyList<ComparisonSessionSummaryDto>>> Handle(
        GetComparisonSessionsQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
        {
            return Result.Failure<IReadOnlyList<ComparisonSessionSummaryDto>>(DomainErrors.Auth.Unauthenticated);
        }

        var ownerId = currentUser.UserId.Value;

        var sessions = await unitOfWork.Repository<ComparisonSession>()
            .FindAsync(s => s.OwnerId == ownerId, cancellationToken);

        var dtos = sessions
            .OrderByDescending(s => s.CreatedAtUtc)
            .Select(s => new ComparisonSessionSummaryDto(
                Id: s.Id,
                ComparisonType: s.ComparisonType.ToString(),
                DocumentIds: s.DocumentIds.ToList().AsReadOnly(),
                Status: s.Status.ToString(),
                ExecutiveOverview: s.ExecutiveOverview,
                CreatedAt: new DateTimeOffset(s.CreatedAtUtc, TimeSpan.Zero)))
            .ToList();

        return Result.Success<IReadOnlyList<ComparisonSessionSummaryDto>>(dtos);
    }
}
