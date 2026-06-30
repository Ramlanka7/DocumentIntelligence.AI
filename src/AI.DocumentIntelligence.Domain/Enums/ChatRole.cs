namespace AI.DocumentIntelligence.Domain.Enums;

/// <summary>Author of a <see cref="Entities.ChatMessage"/> in a document chat session.</summary>
public enum ChatRole
{
    /// <summary>A message authored by the end user.</summary>
    User = 0,

    /// <summary>A message authored by the AI assistant (RAG-grounded, with citations).</summary>
    Assistant = 1,

    /// <summary>A system/instruction message.</summary>
    System = 2,
}
