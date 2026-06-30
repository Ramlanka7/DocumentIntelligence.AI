namespace AI.DocumentIntelligence.Application.Contracts.Documents;

/// <summary>
/// Descriptive metadata extracted from an uploaded document.
/// </summary>
/// <param name="FileName">Original file name.</param>
/// <param name="ContentType">MIME content type.</param>
/// <param name="SizeBytes">File size in bytes.</param>
/// <param name="PageCount">Number of pages detected.</param>
/// <param name="Title">Document title, if available.</param>
/// <param name="Author">Document author, if available.</param>
public sealed record DocumentMetadata(
    string FileName,
    string ContentType,
    long SizeBytes,
    int PageCount,
    string? Title,
    string? Author);
