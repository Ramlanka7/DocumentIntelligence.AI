namespace AI.DocumentIntelligence.Application.Abstractions.Documents;

/// <summary>
/// Represents a single file submitted for upload. The <see cref="Stream"/> is positioned at the
/// beginning so that <see cref="IFileUploadValidator"/> can read magic bytes for content sniffing.
/// </summary>
/// <param name="FileName">The original client-supplied filename.</param>
/// <param name="ContentType">The MIME type declared by the client (used as a hint only).</param>
/// <param name="SizeBytes">The total byte length of the file.</param>
/// <param name="PageCount">The page count reported by the caller (0 when unknown).</param>
/// <param name="Stream">A readable, seekable stream for magic-byte sniffing.</param>
public sealed record UploadedFile(
    string FileName,
    string ContentType,
    long SizeBytes,
    int PageCount,
    Stream Stream);
