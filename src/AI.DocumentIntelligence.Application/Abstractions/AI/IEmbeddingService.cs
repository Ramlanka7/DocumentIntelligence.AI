using AI.DocumentIntelligence.Domain.Common;

namespace AI.DocumentIntelligence.Application.Abstractions.AI;

/// <summary>
/// Generates embedding vectors for text, used to index document chunks and to embed queries for
/// vector search in the RAG pipeline.
/// </summary>
public interface IEmbeddingService
{
    /// <summary>Generates an embedding vector for a single input string.</summary>
    /// <param name="input">The text to embed.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The embedding vector, or a failure <see cref="Result"/>.</returns>
    public Task<Result<IReadOnlyList<float>>> GenerateEmbeddingAsync(
        string input,
        CancellationToken cancellationToken = default);

    /// <summary>Generates embedding vectors for a batch of inputs, preserving order.</summary>
    /// <param name="inputs">The texts to embed.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>One vector per input, in the same order, or a failure <see cref="Result"/>.</returns>
    public Task<Result<IReadOnlyList<IReadOnlyList<float>>>> GenerateEmbeddingsAsync(
        IReadOnlyList<string> inputs,
        CancellationToken cancellationToken = default);
}
