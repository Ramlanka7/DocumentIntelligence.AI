using AI.DocumentIntelligence.Domain.Common;
using AI.DocumentIntelligence.Domain.ValueObjects;

namespace AI.DocumentIntelligence.Domain.Entities;

/// <summary>
/// An append-only ledger entry recording the token usage, cost, and processing time of a single
/// AI call, feeding the Admin Dashboard's AI Usage / AI Cost / Average Processing Time metrics.
/// </summary>
public sealed class AiUsageMetric : BaseEntity
{
    private AiUsageMetric()
    {
        // EF / serialization constructor.
        OperationType = string.Empty;
        TokenUsage = ValueObjects.TokenUsage.None;
    }

    private AiUsageMetric(
        Guid id,
        Guid userId,
        string operationType,
        Guid? sessionId,
        TokenUsage tokenUsage,
        TimeSpan processingTime)
        : base(id)
    {
        UserId = userId;
        OperationType = operationType;
        SessionId = sessionId;
        TokenUsage = tokenUsage;
        ProcessingTime = processingTime;
    }

    public Guid UserId { get; private set; }

    /// <summary>The kind of AI operation, e.g. <c>"Analysis"</c>, <c>"Comparison"</c>, or <c>"Chat"</c>.</summary>
    public string OperationType { get; private set; }

    /// <summary>The id of the related <see cref="AnalysisSession"/>, <see cref="ComparisonSession"/>, or <see cref="ChatSession"/>, if any.</summary>
    public Guid? SessionId { get; private set; }

    public TokenUsage TokenUsage { get; private set; }

    public TimeSpan ProcessingTime { get; private set; }

    public static AiUsageMetric Create(
        Guid userId,
        string operationType,
        TokenUsage tokenUsage,
        TimeSpan processingTime,
        Guid? sessionId = null) =>
        new(Guid.NewGuid(), userId, operationType, sessionId, tokenUsage, processingTime);
}
