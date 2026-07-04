using AI.DocumentIntelligence.Application.Contracts;

namespace AI.DocumentIntelligence.Application.Features.RAG.Search;

/// <summary>
/// The result of a <see cref="SearchDocumentsQuery"/> — ranked chunks together with their
/// citations so that downstream AI services can ground responses in source evidence.
/// </summary>
/// <param name="Chunks">Ranked, retrieved text chunks.</param>
/// <param name="Citations">
/// One <see cref="Citation"/> per chunk, preserving document name, page, paragraph and
/// confidence score (mapped from the search engine relevance score).
/// </param>
public sealed record SearchDocumentsResponse(
    IReadOnlyList<RetrievedChunk> Chunks,
    IReadOnlyList<Citation> Citations);
