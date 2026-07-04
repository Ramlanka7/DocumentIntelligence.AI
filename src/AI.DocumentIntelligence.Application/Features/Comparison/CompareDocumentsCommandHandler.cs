using AI.DocumentIntelligence.Application.Abstractions.AI;
using AI.DocumentIntelligence.Application.Common.Messaging;
using AI.DocumentIntelligence.Application.Contracts.Comparison;
using AI.DocumentIntelligence.Domain.Common;

namespace AI.DocumentIntelligence.Application.Features.Comparison;

/// <summary>Delegates to <see cref="IComparisonService"/> and forwards the result.</summary>
internal sealed class CompareDocumentsCommandHandler(IComparisonService comparisonService)
    : ICommandHandler<CompareDocumentsCommand, ComparisonResult>
{
    public async Task<Result<ComparisonResult>> Handle(
        CompareDocumentsCommand request,
        CancellationToken cancellationToken)
    {
        var comparisonRequest = new ComparisonRequest(
            request.DocumentIds,
            request.ComparisonType,
            request.CustomInstructions);

        return await comparisonService.CompareAsync(comparisonRequest, cancellationToken);
    }
}
