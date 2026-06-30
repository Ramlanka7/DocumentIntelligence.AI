using AI.DocumentIntelligence.Domain.Common;
using AI.DocumentIntelligence.Domain.Enums;
using AI.DocumentIntelligence.Domain.Errors;
using AI.DocumentIntelligence.Domain.ValueObjects;

namespace AI.DocumentIntelligence.Domain.Entities;

/// <summary>
/// An AI-driven comparison across 2-4 documents for a single <see cref="ComparisonType"/>,
/// producing a structured diff-style result with a detailed change log and citations.
/// </summary>
public sealed class ComparisonSession : AuditableEntity
{
    private const int MinDocuments = 2;
    private const int MaxDocuments = 4;

    private readonly List<Guid> _documentIds = [];
    private readonly List<string> _keyDifferences = [];
    private readonly List<string> _recommendations = [];
    private readonly List<ChangeLogEntry> _detailedChangeLog = [];
    private readonly List<Citation> _sourceCitations = [];

    private ComparisonSession()
    {
        // EF / serialization constructor.
        TokenUsage = ValueObjects.TokenUsage.None;
    }

    private ComparisonSession(Guid id, Guid ownerId, IEnumerable<Guid> documentIds, ComparisonType comparisonType)
        : base(id)
    {
        OwnerId = ownerId;
        _documentIds.AddRange(documentIds);
        ComparisonType = comparisonType;
        Status = SessionStatus.Pending;
        TokenUsage = ValueObjects.TokenUsage.None;
    }

    public Guid OwnerId { get; private set; }

    public IReadOnlyCollection<Guid> DocumentIds => _documentIds.AsReadOnly();

    public ComparisonType ComparisonType { get; private set; }

    public SessionStatus Status { get; private set; }

    public string? ExecutiveOverview { get; private set; }

    public IReadOnlyCollection<string> KeyDifferences => _keyDifferences.AsReadOnly();

    public string? RiskAnalysis { get; private set; }

    public IReadOnlyCollection<string> Recommendations => _recommendations.AsReadOnly();

    public IReadOnlyCollection<ChangeLogEntry> DetailedChangeLog => _detailedChangeLog.AsReadOnly();

    public IReadOnlyCollection<Citation> SourceCitations => _sourceCitations.AsReadOnly();

    public TokenUsage TokenUsage { get; private set; }

    public TimeSpan? ProcessingTime { get; private set; }

    public string? FailureReason { get; private set; }

    /// <summary>Creates a new comparison session, validating the document count invariant (2-4 documents).</summary>
    public static Result<ComparisonSession> Create(Guid ownerId, IEnumerable<Guid> documentIds, ComparisonType comparisonType)
    {
        var ids = documentIds.ToList();

        if (ids.Count is < MinDocuments or > MaxDocuments)
        {
            return Result.Failure<ComparisonSession>(DomainErrors.Comparison.InsufficientDocuments);
        }

        return Result.Success(new ComparisonSession(Guid.NewGuid(), ownerId, ids, comparisonType));
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

    /// <summary>Records a successful comparison outcome and transitions the session to <see cref="SessionStatus.Completed"/>.</summary>
    public Result Complete(
        string executiveOverview,
        IEnumerable<string> keyDifferences,
        string riskAnalysis,
        IEnumerable<string> recommendations,
        IEnumerable<ChangeLogEntry> detailedChangeLog,
        IEnumerable<Citation> sourceCitations,
        TokenUsage tokenUsage,
        TimeSpan processingTime)
    {
        if (Status != SessionStatus.InProgress)
        {
            return Result.Failure(DomainErrors.Session.InvalidStateTransition);
        }

        ExecutiveOverview = executiveOverview;

        _keyDifferences.Clear();
        _keyDifferences.AddRange(keyDifferences);

        RiskAnalysis = riskAnalysis;

        _recommendations.Clear();
        _recommendations.AddRange(recommendations);

        _detailedChangeLog.Clear();
        _detailedChangeLog.AddRange(detailedChangeLog);

        _sourceCitations.Clear();
        _sourceCitations.AddRange(sourceCitations);

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
