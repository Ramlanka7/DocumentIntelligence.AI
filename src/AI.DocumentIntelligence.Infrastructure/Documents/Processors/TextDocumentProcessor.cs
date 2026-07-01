using AI.DocumentIntelligence.Application.Abstractions.Documents;
using AI.DocumentIntelligence.Application.Contracts.Documents;
using AI.DocumentIntelligence.Domain.Common;

namespace AI.DocumentIntelligence.Infrastructure.Documents.Processors;

/// <summary>
/// Extracts text and headings from plain-text documents.
/// Pages are estimated at 50 lines each; headings are lines that are all-caps or short and end with a colon.
/// </summary>
internal sealed class TextDocumentProcessor : IDocumentProcessor
{
    private const int LinesPerPage = 50;
    private const int MaxHeadingLength = 80;

    public bool CanProcess(string fileName, string contentType)
        => Path.GetExtension(fileName).Equals(".txt", StringComparison.OrdinalIgnoreCase)
           || contentType.Equals("text/plain", StringComparison.OrdinalIgnoreCase);

    public async Task<Result<DocumentExtractionResult>> ProcessAsync(
        Stream content,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var reader = new StreamReader(content, leaveOpen: true);
            var text = await reader.ReadToEndAsync(cancellationToken);
            return Extract(text, fileName, contentType, content.CanSeek ? content.Length : 0L);
        }
        catch (Exception ex)
        {
            return Result.Failure<DocumentExtractionResult>(
                Error.Failure("Document.Text.ProcessingFailed", $"Failed to process text file: {ex.Message}"));
        }
    }

    private static Result<DocumentExtractionResult> Extract(
        string text,
        string fileName,
        string contentType,
        long sizeBytes)
    {
        var lines = text.Split('\n');
        var pages = BuildPages(lines);
        var sections = BuildSections(lines, pages);

        var metadata = new DocumentMetadata(
            fileName,
            contentType,
            sizeBytes,
            pages.Count,
            null,
            null);

        return Result.Success(new DocumentExtractionResult(
            text,
            pages,
            sections,
            [],
            metadata));
    }

    private static List<ExtractedPage> BuildPages(string[] lines)
    {
        var pages = new List<ExtractedPage>();
        var pageNumber = 1;

        for (var i = 0; i < lines.Length; i += LinesPerPage)
        {
            var pageLines = lines.Skip(i).Take(LinesPerPage);
            var pageText = string.Join("\n", pageLines);
            if (!string.IsNullOrWhiteSpace(pageText))
            {
                pages.Add(new ExtractedPage(pageNumber++, pageText));
            }
        }

        if (pages.Count == 0)
        {
            pages.Add(new ExtractedPage(1, string.Empty));
        }

        return pages;
    }

    private static List<ExtractedSection> BuildSections(string[] lines, List<ExtractedPage> pages)
    {
        var sections = new List<ExtractedSection>();
        string? currentHeading = null;
        var currentStartPage = 1;
        var contentLines = new List<string>();

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i].TrimEnd();
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var estimatedPage = Math.Max(1, i / LinesPerPage + 1);
            var currentPage = pages.Count >= estimatedPage
                ? estimatedPage
                : pages.Count;

            if (IsHeading(line))
            {
                if (currentHeading is not null)
                {
                    sections.Add(new ExtractedSection(
                        currentHeading,
                        1,
                        currentStartPage,
                        string.Join(" ", contentLines)));
                    contentLines.Clear();
                }

                currentHeading = line;
                currentStartPage = currentPage;
            }
            else
            {
                contentLines.Add(line);
            }
        }

        if (currentHeading is not null)
        {
            sections.Add(new ExtractedSection(
                currentHeading,
                1,
                currentStartPage,
                string.Join(" ", contentLines)));
        }

        return sections;
    }

    private static bool IsHeading(string line)
    {
        if (line.Length > MaxHeadingLength)
        {
            return false;
        }

        var trimmed = line.Trim();
        if (trimmed.Length >= 3 && string.Equals(trimmed, trimmed.ToUpperInvariant(), StringComparison.Ordinal))
        {
            return true;
        }

        if (trimmed.EndsWith(':'))
        {
            return trimmed.Split(' ').Length <= 6;
        }

        return false;
    }
}
