using AI.DocumentIntelligence.Application.Abstractions.Documents;
using AI.DocumentIntelligence.Domain.Common;
using AI.DocumentIntelligence.Domain.Errors;
using Microsoft.Extensions.Options;

namespace AI.DocumentIntelligence.Infrastructure.Auth;

/// <summary>
/// Validates a batch of uploaded files by inspecting magic bytes (content sniffing),
/// enforcing per-file size, combined size, document count, and combined page count limits.
/// Never throws; always returns a <see cref="Result"/>.
/// </summary>
internal sealed class FileUploadValidator(IOptions<UploadOptions> options) : IFileUploadValidator
{
    private readonly UploadOptions _options = options.Value;

    // Magic byte signatures for supported types.
    private static readonly byte[] PdfMagic = [0x25, 0x50, 0x44, 0x46]; // %PDF
    private static readonly byte[] ZipMagic = [0x50, 0x4B, 0x03, 0x04]; // PK (DOCX/PPTX/XLSX)
    private static readonly byte[] ZipSpanned = [0x50, 0x4B, 0x07, 0x08]; // PK spanned zip variant

    /// <inheritdoc />
    public Result Validate(IReadOnlyList<UploadedFile> files)
    {
        if (files.Count > _options.MaxDocuments)
        {
            return Result.Failure(DomainErrors.Upload.TooManyDocuments);
        }

        long combinedSize = 0;
        int combinedPages = 0;

        foreach (var file in files)
        {
            // Per-file size check.
            if (file.SizeBytes > _options.MaxFileSizeBytes)
            {
                return Result.Failure(DomainErrors.Upload.FileSizeExceeded);
            }

            combinedSize += file.SizeBytes;

            // Content sniffing via magic bytes.
            if (!IsSupportedFileType(file))
            {
                return Result.Failure(DomainErrors.Upload.UnsupportedFileType);
            }

            combinedPages += file.PageCount;
        }

        if (combinedSize > _options.MaxCombinedSizeBytes)
        {
            return Result.Failure(DomainErrors.Upload.CombinedSizeExceeded);
        }

        if (combinedPages > _options.MaxCombinedPages)
        {
            return Result.Failure(DomainErrors.Upload.CombinedPageLimitExceeded);
        }

        return Result.Success();
    }

    private static bool IsSupportedFileType(UploadedFile file)
    {
        Span<byte> buffer = stackalloc byte[8];

        try
        {
            if (!file.Stream.CanRead || !file.Stream.CanSeek)
            {
                return false;
            }

            long originalPosition = file.Stream.Position;
            file.Stream.Seek(0, SeekOrigin.Begin);
            int bytesRead = file.Stream.Read(buffer);
            file.Stream.Seek(originalPosition, SeekOrigin.Begin);

            // The declared extension and the sniffed magic bytes must AGREE.
            // Accepting any ZIP (or any PDF) regardless of filename would let a
            // renamed archive/executable masquerade as a supported document.
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (bytesRead < 4)
            {
                // Very small files — allow only for declared plain-text types.
                return IsTextExtension(extension) && IsPlainText(file.ContentType);
            }

            return extension switch
            {
                ".pdf" => MatchesMagic(buffer[..4], PdfMagic),

                // DOCX / PPTX — ZIP-based Office Open XML. The document processors
                // fully parse the archive afterwards, rejecting non-OOXML zips.
                ".docx" or ".pptx" =>
                    MatchesMagic(buffer[..4], ZipMagic) || MatchesMagic(buffer[..4], ZipSpanned),

                // Plain text / CSV — declared as text or ASCII/UTF-8 printable content.
                ".txt" or ".csv" =>
                    IsPlainText(file.ContentType) || IsReadableText(buffer[..bytesRead]),

                // Unknown or missing extension — never accepted, whatever the bytes say.
                _ => false,
            };
        }
        catch (Exception)
        {
            // Stream read failures are treated as unsupported.
            return false;
        }
    }

    private static bool IsTextExtension(string extension) =>
        extension is ".txt" or ".csv";

    private static bool MatchesMagic(Span<byte> header, byte[] magic)
    {
        if (header.Length < magic.Length)
        {
            return false;
        }

        for (int i = 0; i < magic.Length; i++)
        {
            if (header[i] != magic[i])
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsPlainText(string contentType)
    {
        var ct = contentType.ToLowerInvariant();
        return ct.StartsWith("text/", StringComparison.Ordinal)
            || ct.Contains("csv", StringComparison.Ordinal)
            || ct.Contains("plain", StringComparison.Ordinal);
    }

    /// <summary>
    /// Heuristic check: if all bytes are printable ASCII or common UTF-8 control characters
    /// (tab, LF, CR), treat the file as plain text.
    /// </summary>
    private static bool IsReadableText(Span<byte> bytes)
    {
        foreach (byte b in bytes)
        {
            if (b < 0x09)
            {
                return false; // non-printable control below tab
            }

            if (b == 0x0B || b == 0x0C)
            {
                return false; // VT, FF — not typical in text files
            }

            if (b > 0x7E && b < 0xC0)
            {
                return false; // Latin-1 supplement but not valid UTF-8 start byte
            }
        }

        return true;
    }
}
