using AI.DocumentIntelligence.Domain.Enums;

namespace AI.DocumentIntelligence.Application.Features.Documents.List;

/// <summary>Summary view of a document for list endpoints.</summary>
/// <param name="Id">The document's unique identifier.</param>
/// <param name="FileName">The original file name.</param>
/// <param name="Status">Current lifecycle status.</param>
/// <param name="Type">The document format.</param>
/// <param name="SizeBytes">File size in bytes.</param>
/// <param name="CreatedAt">When the document was created (UTC).</param>
public sealed record DocumentSummaryDto(
    Guid Id,
    string FileName,
    DocumentStatus Status,
    DocumentType Type,
    long SizeBytes,
    DateTimeOffset CreatedAt);
