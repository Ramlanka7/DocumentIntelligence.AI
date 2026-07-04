using AI.DocumentIntelligence.Application.Abstractions.AI;
using AI.DocumentIntelligence.Application.Contracts.AI;
using AI.DocumentIntelligence.Domain.Common;

namespace AI.DocumentIntelligence.Tests.Integration.Fakes;

/// <summary>
/// Deterministic AI provider stub that returns a valid JSON payload for every request.
/// Used in integration tests to avoid any real network calls to Azure OpenAI.
/// </summary>
public sealed class StubAIProvider : IAIProvider
{
    public string Name => "Stub";

    public Task<Result<AiCompletionResult>> CompleteAsync(
        AiCompletionRequest request,
        CancellationToken cancellationToken = default)
    {
        // Return the minimal JSON shapes expected by AnalysisService and ComparisonService.
        // The service embeds the intent in the System role message.
        var systemContent = request.Messages
            .FirstOrDefault(m => m.Role == AiRole.System)?.Content ?? string.Empty;

        var json = systemContent.Contains("comparison", StringComparison.OrdinalIgnoreCase)
            ? ComparisonJson()
            : AnalysisJson();

        var result = new AiCompletionResult(
            json,
            new TokenUsage(100, 50, 0.001m),
            Name);

        return Task.FromResult(Result.Success(result));
    }

    private static string AnalysisJson() => """
        {
          "executiveSummary": "Stub summary",
          "keyFindings": [],
          "risks": [],
          "recommendations": [],
          "actionItems": [],
          "sources": []
        }
        """;

    private static string ComparisonJson() => """
        {
          "overview": "Stub comparison overview",
          "differences": [],
          "similarities": [],
          "changeLog": [],
          "sources": []
        }
        """;
}
