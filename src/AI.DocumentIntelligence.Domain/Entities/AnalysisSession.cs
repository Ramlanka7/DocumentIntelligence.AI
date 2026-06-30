using AI.DocumentIntelligence.Domain.Common;
using AI.DocumentIntelligence.Domain.Enums;
using AI.DocumentIntelligence.Domain.Errors;
using AI.DocumentIntelligence.Domain.ValueObjects;

namespace AI.DocumentIntelligence.Domain.Entities;

/// <summary>
/// An AI-driven analysis run over 1-4 documents for a single <see cref="AnalysisCapability"/>,
/// producing structured results with citations back to the source documents.
/// </summary>
public sealed class AnalysisSession : AuditableEntity
{
    private const int MaxDocuments = 4;

    private readonly List<Guid> _documentIds = [];
    private readonly List<string> _keyFindings = [];
    private readonly List<string> _risksIdentified = [];
    private readonly List<string> _recommendations = [];
    private readonly List<string> _actionItems = [];
    private readonly List<Citation> _referencedSources = [];

    private AnalysisSession()
    {
        // EF / serialization constructor.
        TokenUsage = ValueObjects.TokenUsage.None;
    }

    private AnalysisSession(
        Guid id,
        Guid ownerId,
        IEnumerable<Guid> documentIds,
        AnalysisCapability capability,
        string? customQuestion)
        : base(id)
    {
        OwnerId = ownerId;
        _documentIds.AddRange(documentIds);
        Capability = capability;
        CustomQuestion = customQuestion;
        Status = SessionStatus.Pending;
        TokenUsage = ValueObjects.TokenUsage.None;
    }

    public Guid OwnerId { get; private set; }

    public IReadOnlyCollection<Guid> DocumentIds => _documentIds.AsReadOnly();

    public AnalysisCapability Capability { get; private set; }

    /// <summary>The user-supplied question; only meaningful when <see cref="Capability"/> is <see cref="AnalysisCapability.CustomQuestion"/>.</summary>
    public string? CustomQuestion { get; private set; }

    public SessionStatus Status { get; private set; }

    public string? ExecutiveSummary { get; private set; }

    public IReadOnlyCollection<string> KeyFindings => _keyFindings.AsReadOnly();

    public IReadOnlyCollection<string> RisksIdentified => _risksIdentified.AsReadOnly();

    public IReadOnlyCollection<string> Recommendations => _recommendations.AsReadOnly();

    public IReadOnlyCollection<string> ActionItems => _actionItems.AsReadOnly();

    public IReadOnlyCollection<Citation> ReferencedSources => _referencedSources.AsReadOnly();

    public TokenUsage TokenUsage { get; private set; }

    public TimeSpan? ProcessingTime { get; private set; }

    public string? FailureReason { get; private set; }

    /// <summary>Creates a new analysis session, validating the document count invariant (1-4 documents).</summary>
    public static Result<AnalysisSession> Create(
        Guid ownerId,
        IEnumerable<Guid> documentIds,
        AnalysisCapability capability,
        string? customQuestion = null)
    {
        var ids = documentIds.ToList();

        if (ids.Count == 0)
        {
            return Result.Failure<AnalysisSession>(DomainErrors.Analysis.NoDocuments);
        }

        if (ids.Count > MaxDocuments)
        {
            return Result.Failure<AnalysisSession>(DomainErrors.Analysis.TooManyDocuments);
        }

        return Result.Success(new AnalysisSession(Guid.NewGuid(), ownerId, ids, capability, customQuestion));
    }

    public Result MarkInProgress()
    {
        if (Status != SessionStatus.Pending)
        {
            return Result.Failure(DomainErrors.Session.InvalidStateTransition);
        }

        Status = SessionStatus.InProgress;
        return Result.Success();
    }

    /// <summary>Records a successful analysis outcome and transitions the session to <see cref="SessionStatus.Completed"/>.</summary>
    public Result Complete(
        string executiveSummary,
        IEnumerable<string> keyFindings,
        IEnumerable<string> risksIdentified,
        IEnumerable<string> recommendations,
        IEnumerable<string> actionItems,
        IEnumerable<Citation> referencedSources,
        TokenUsage tokenUsage,
        TimeSpan processingTime)
    {
        if (Status != SessionStatus.InProgress)
        {
            return Result.Failure(DomainErrors.Session.InvalidStateTransition);
        }

        ExecutiveSummary = executiveSummary;

        _keyFindings.Clear();
        _keyFindings.AddRange(keyFindings);

        _risksIdentified.Clear();
        _risksIdentified.AddRange(risksIdentified);

        _recommendations.Clear();
        _recommendations.AddRange(recommendations);

        _actionItems.Clear();
        _actionItems.AddRange(actionItems);

        _referencedSources.Clear();
        _referencedSources.AddRange(referencedSources);

        TokenUsage = tokenUsage;
        ProcessingTime = processingTime;
        FailureReason = null;
        Status = SessionStatus.Completed;
        return Result.Success();
    }

    public Result MarkFailed(string reason)
    {
        if (Status is SessionStatus.Completed or SessionStatus.Failed)
        {
            return Result.Failure(DomainErrors.Session.InvalidStateTransition);
        }

        FailureReason = reason;
        Status = SessionStatus.Failed;
        return Result.Success();
    }
}
