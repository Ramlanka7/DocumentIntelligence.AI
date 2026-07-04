using AI.DocumentIntelligence.Application.Abstractions.AI;
using AI.DocumentIntelligence.Application.Common.Messaging;
using AI.DocumentIntelligence.Application.Contracts.Analysis;
using AI.DocumentIntelligence.Domain.Common;

namespace AI.DocumentIntelligence.Application.Features.Analysis;

/// <summary>Delegates to <see cref="IAnalysisService"/> and forwards the result.</summary>
internal sealed class AnalyzeDocumentsCommandHandler(IAnalysisService analysisService)
    : ICommandHandler<AnalyzeDocumentsCommand, AnalysisResult>
{
    public async Task<Result<AnalysisResult>> Handle(
        AnalyzeDocumentsCommand request,
        CancellationToken cancellationToken)
    {
        var analysisRequest = new AnalysisRequest(
            request.DocumentIds,
            request.Capability,
            request.CustomQuestion);

        return await analysisService.AnalyzeAsync(analysisRequest, cancellationToken);
    }
}
