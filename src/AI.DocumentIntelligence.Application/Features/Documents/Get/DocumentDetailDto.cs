using AI.DocumentIntelligence.Domain.Enums;

namespace AI.DocumentIntelligence.Application.Features.Documents.Queries;

/// <summary>Detailed view of a document for single-document endpoints.</summary>
/// <param name="Id">The document's unique identifier.</param>
/// <param name="FileName">The original file name.</param>
/// <param name="Status">Current lifecycle status.</param>
/// <param name="Type">The document format.</param>
/// <param name="SizeBytes">File size in bytes.</param>
/// <param name="PageCount">Number of pages detected during extraction.</param>
/// <param name="CreatedAt">When the document was created (UTC).</param>
/// <param name="FailureReason">Processing failure message, if applicable.</param>
public sealed record DocumentDetailDto(
    Guid Id,
    string FileName,
    DocumentStatus Status,
    DocumentType Type,
    long SizeBytes,
    int PageCount,
    DateTimeOffset CreatedAt,
    string? FailureReason);
