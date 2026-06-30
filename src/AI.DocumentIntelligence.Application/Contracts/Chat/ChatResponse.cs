using AI.DocumentIntelligence.Application.Contracts;
using AI.DocumentIntelligence.Application.Contracts.AI;

namespace AI.DocumentIntelligence.Application.Contracts.Chat;

/// <summary>An AI answer to a chat question, grounded in retrieved sources.</summary>
/// <param name="Answer">The generated answer text.</param>
/// <param name="Citations">Sources supporting the answer (required for every response).</param>
/// <param name="Usage">Token usage for the call.</param>
public sealed record ChatResponse(
    string Answer,
    IReadOnlyList<Citation> Citations,
    TokenUsage Usage);
