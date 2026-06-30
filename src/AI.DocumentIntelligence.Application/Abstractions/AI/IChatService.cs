using AI.DocumentIntelligence.Application.Contracts.Chat;
using AI.DocumentIntelligence.Domain.Common;

namespace AI.DocumentIntelligence.Application.Abstractions.AI;

/// <summary>
/// Answers conversational questions grounded in uploaded documents via RAG retrieval, always
/// returning source citations alongside the answer.
/// </summary>
public interface IChatService
{
    /// <summary>Answers a question within a document-scoped chat session.</summary>
    /// <param name="request">The chat request, including history and document scope.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The grounded answer with citations, or a failure <see cref="Result"/>.</returns>
    public Task<Result<ChatResponse>> AskAsync(
        ChatRequest request,
        CancellationToken cancellationToken = default);
}
