using AI.DocumentIntelligence.Domain.Common;
using AI.DocumentIntelligence.Domain.Enums;
using AI.DocumentIntelligence.Domain.ValueObjects;

namespace AI.DocumentIntelligence.Domain.Entities;

/// <summary>
/// A single turn in a <see cref="ChatSession"/>. Assistant turns are RAG-grounded and carry
/// citations and token usage; user/system turns do not.
/// </summary>
public sealed class ChatMessage : BaseEntity
{
    private readonly List<Citation> _citations = [];

    private ChatMessage()
    {
        // EF / serialization constructor.
        Content = string.Empty;
        TokenUsage = ValueObjects.TokenUsage.None;
    }

    private ChatMessage(Guid id, Guid chatSessionId, int ordinal, ChatRole role, string content)
        : base(id)
    {
        ChatSessionId = chatSessionId;
        Ordinal = ordinal;
        Role = role;
        Content = content;
        TokenUsage = ValueObjects.TokenUsage.None;
    }

    public Guid ChatSessionId { get; private set; }

    /// <summary>Zero-based position of the message within its session.</summary>
    public int Ordinal { get; private set; }

    public ChatRole Role { get; private set; }

    public string Content { get; private set; }

    /// <summary>Source citations grounding the reply; only meaningful for <see cref="ChatRole.Assistant"/> messages.</summary>
    public IReadOnlyCollection<Citation> Citations => _citations.AsReadOnly();

    /// <summary>Token usage for the AI call that produced this message; <see cref="ValueObjects.TokenUsage.None"/> for non-assistant turns.</summary>
    public TokenUsage TokenUsage { get; private set; }

    public static ChatMessage Create(Guid chatSessionId, int ordinal, ChatRole role, string content) =>
        new(Guid.NewGuid(), chatSessionId, ordinal, role, content);

    public void AddCitation(Citation citation) => _citations.Add(citation);

    public void SetTokenUsage(TokenUsage tokenUsage) => TokenUsage = tokenUsage;
}
