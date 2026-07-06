using AI.DocumentIntelligence.Application.Contracts.Documents;

namespace AI.DocumentIntelligence.Application.Abstractions.Ingestion;

/// <summary>
/// A queued RAG-ingestion request: chunk → embed → index for one uploaded document,
/// plus the data needed to finalize the document's status afterwards.
/// </summary>
/// <param name="DocumentId">The persisted document identifier.</param>
/// <param name="DocumentName">Human-readable file name, used in citations.</param>
/// <param name="ExtractionResult">Structured extraction output from the document processor.</param>
public sealed record IngestionJob(
    Guid DocumentId,
    string DocumentName,
    DocumentExtractionResult ExtractionResult);

/// <summary>
/// Decouples document upload from RAG ingestion. Upload extracts text, stores the file, and
/// enqueues a job; a background worker performs chunking/embedding/indexing and moves the
/// document from <c>Processing</c> to <c>Processed</c> or <c>Failed</c>. This keeps large
/// uploads from holding HTTP requests open through embedding calls.
/// </summary>
public interface IIngestionScheduler
{
    /// <summary>Schedules a job for background ingestion. Waits when the backlog is full.</summary>
    public ValueTask ScheduleAsync(IngestionJob job, CancellationToken cancellationToken = default);
}
