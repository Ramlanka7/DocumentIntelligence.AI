using AI.DocumentIntelligence.Application.Common.Messaging;
using AI.DocumentIntelligence.Application.Contracts.Documents;

namespace AI.DocumentIntelligence.Application.Features.RAG.Ingest;

/// <summary>
/// Orchestrates the ingestion pipeline for a single document:
/// extract (delegate to the caller) → chunk → embed → index into Azure AI Search.
/// The caller supplies the already-extracted result so that extraction concerns
/// (streaming, storage) are separated from the RAG indexing pipeline.
/// </summary>
/// <param name="DocumentId">The persisted document identifier.</param>
/// <param name="DocumentName">Human-readable file name, used in citations.</param>
/// <param name="ExtractionResult">Structured extraction output from the document processor.</param>
public sealed record IngestDocumentCommand(
    Guid DocumentId,
    string DocumentName,
    DocumentExtractionResult ExtractionResult) : ICommand;
