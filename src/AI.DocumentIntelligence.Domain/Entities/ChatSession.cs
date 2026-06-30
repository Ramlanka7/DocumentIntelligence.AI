using AI.DocumentIntelligence.Domain.Common;
using AI.DocumentIntelligence.Domain.Enums;
using AI.DocumentIntelligence.Domain.Errors;

namespace AI.DocumentIntelligence.Domain.Entities;

/// <summary>
/// A conversational, RAG-grounded chat session scoped to one or more documents. Owns the
/// ordered collection of <see cref="ChatMessage"/> turns.
/// </summary>
public sealed class ChatSession : AuditableEntity
{
    private readonly List<Guid> _documentIds = [];
    private readonly List<ChatMessage> _messages = [];

    private ChatSession()
    {
        // EF / serialization constructor.
    }

    private ChatSession(Guid id, Guid ownerId, IEnumerable<Guid> documentIds)
        : base(id)
    {
        OwnerId = ownerId;
        _documentIds.AddRange(documentIds);
        Status = SessionStatus.Pending;
    }

    public Guid OwnerId { get; private set; }

    public IReadOnlyCollection<Guid> DocumentIds => _documentIds.AsReadOnly();

    public SessionStatus Status { get; private set; }

    public IReadOnlyCollection<ChatMessage> Messages => _messages.AsReadOnly();

    public static Result<ChatSession> Create(Guid ownerId, IEnumerable<Guid> documentIds)
    {
        var ids = documentIds.ToList();

        if (ids.Count == 0)
        {
            return Result.Failure<ChatSession>(DomainErrors.Chat.NoDocuments);
        }

        return Result.Success(new ChatSession(Guid.NewGuid(), ownerId, ids));
    }

    /// <summary>Appends a message at the next ordinal position and returns it.</summary>
    public ChatMessage AddMessage(ChatRole role, string content)
    {
        var message = ChatMessage.Create(Id, _messages.Count, role, content);
        _messages.Add(message);

        if (Status == SessionStatus.Pending)
        {
            Status = SessionStatus.InProgress;
        }

        return message;
    }

    public Result Close()
    {
        if (Status == SessionStatus.Failed)
        {
            return Result.Failure(DomainErrors.Session.InvalidStateTransition);
        }

        Status = SessionStatus.Completed;
        return Result.Success();
    }

    public Result MarkFailed()
    {
        if (Status is SessionStatus.Completed or SessionStatus.Failed)
        {
            return Result.Failure(DomainErrors.Session.InvalidStateTransition);
        }

        Status = SessionStatus.Failed;
        return Result.Success();
    }
}
