using AI.DocumentIntelligence.Application.Abstractions.Identity;
using AI.DocumentIntelligence.Application.Abstractions.Persistence;
using AI.DocumentIntelligence.Application.Common.Messaging;
using AI.DocumentIntelligence.Domain.Common;
using AI.DocumentIntelligence.Domain.Entities;
using AI.DocumentIntelligence.Domain.Errors;

namespace AI.DocumentIntelligence.Application.Features.Analysis.GetAnalysisSessions;

/// <summary>Returns the current user's analysis sessions, owner-scoped for security.</summary>
internal sealed class GetAnalysisSessionsQueryHandler(
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser)
    : IQueryHandler<GetAnalysisSessionsQuery, IReadOnlyList<AnalysisSessionSummaryDto>>
{
    public async Task<Result<IReadOnlyList<AnalysisSessionSummaryDto>>> Handle(
        GetAnalysisSessionsQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
        {
            return Result.Failure<IReadOnlyList<AnalysisSessionSummaryDto>>(DomainErrors.Auth.Unauthenticated);
        }

        var ownerId = currentUser.UserId.Value;

        var sessions = await unitOfWork.Repository<AnalysisSession>()
            .FindAsync(s => s.OwnerId == ownerId, cancellationToken);

        var dtos = sessions
            .OrderByDescending(s => s.CreatedAtUtc)
            .Select(s => new AnalysisSessionSummaryDto(
                Id: s.Id,
                Capability: s.Capability.ToString(),
                DocumentIds: s.DocumentIds.ToList().AsReadOnly(),
                Status: s.Status.ToString(),
                ExecutiveSummary: s.ExecutiveSummary,
                CreatedAt: new DateTimeOffset(s.CreatedAtUtc, TimeSpan.Zero)))
            .ToList();

        return Result.Success<IReadOnlyList<AnalysisSessionSummaryDto>>(dtos);
    }
}
