namespace AI.DocumentIntelligence.Domain.ValueObjects;

/// <summary>Descriptive metadata captured for an uploaded document file.</summary>
/// <param name="FileName">The original file name as uploaded.</param>
/// <param name="SizeBytes">The file size in bytes.</param>
/// <param name="PageCount">The number of pages (or rows/slides) detected.</param>
/// <param name="ContentType">The MIME content type, e.g. <c>application/pdf</c>.</param>
public sealed record FileMetadata(
    string FileName,
    long SizeBytes,
    int PageCount,
    string ContentType);
