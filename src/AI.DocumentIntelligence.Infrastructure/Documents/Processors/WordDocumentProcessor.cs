using AI.DocumentIntelligence.Application.Abstractions.Documents;
using AI.DocumentIntelligence.Application.Contracts.Documents;
using AI.DocumentIntelligence.Domain.Common;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace AI.DocumentIntelligence.Infrastructure.Documents.Processors;

/// <summary>
/// Extracts text, headings/sections, tables, and metadata from DOCX documents
/// using the Open XML SDK. Page numbers are estimated from accumulated word count.
/// </summary>
internal sealed class WordDocumentProcessor : IDocumentProcessor
{
    private const int WordsPerPage = 500;
    private const string DocxContentType =
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

    public bool CanProcess(string fileName, string contentType)
        => Path.GetExtension(fileName).Equals(".docx", StringComparison.OrdinalIgnoreCase)
           || contentType.Equals(DocxContentType, StringComparison.OrdinalIgnoreCase);

    public async Task<Result<DocumentExtractionResult>> ProcessAsync(
        Stream content,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await Task.Run(() => Extract(content, fileName, contentType), cancellationToken);
        }
        catch (Exception ex)
        {
            return Result.Failure<DocumentExtractionResult>(
                Error.Failure("Document.Docx.ProcessingFailed", $"Failed to process DOCX: {ex.Message}"));
        }
    }

    private static Result<DocumentExtractionResult> Extract(Stream content, string fileName, string contentType)
    {
        using var wordDoc = WordprocessingDocument.Open(content, false);
        var body = wordDoc.MainDocumentPart?.Document?.Body;
        if (body is null)
        {
            return Result.Failure<DocumentExtractionResult>(
                Error.Failure("Document.Docx.EmptyDocument", "The DOCX document has no body content."));
        }

        var paragraphs = body.Elements<Paragraph>().ToList();
        var sections = new List<ExtractedSection>();
        var pageTexts = new Dictionary<int, List<string>>();
        var fullTextParts = new List<string>();

        var wordCount = 0;
        var currentPage = 1;

        string? currentHeading = null;
        var currentLevel = 1;
        var currentStartPage = 1;
        var sectionContentParts = new List<string>();

        foreach (var para in paragraphs)
        {
            var styleId = para.ParagraphProperties?.ParagraphStyleId?.Val?.Value ?? string.Empty;
            var paraText = para.InnerText;

            if (string.IsNullOrWhiteSpace(paraText))
            {
                continue;
            }

            if (HasPageBreak(para))
            {
                currentPage++;
            }

            if (!pageTexts.TryGetValue(currentPage, out var pageList))
            {
                pageList = [];
                pageTexts[currentPage] = pageList;
            }

            pageList.Add(paraText);
            fullTextParts.Add(paraText);

            wordCount += paraText.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
            if (wordCount >= WordsPerPage && !HasPageBreak(para))
            {
                currentPage++;
                wordCount = 0;
            }

            if (styleId.StartsWith("Heading", StringComparison.OrdinalIgnoreCase))
            {
                if (currentHeading is not null)
                {
                    sections.Add(new ExtractedSection(
                        currentHeading,
                        currentLevel,
                        currentStartPage,
                        string.Join(" ", sectionContentParts)));
                    sectionContentParts.Clear();
                }

                currentHeading = paraText;
                currentLevel = ParseHeadingLevel(styleId);
                currentStartPage = currentPage;
            }
            else
            {
                sectionContentParts.Add(paraText);
            }
        }

        if (currentHeading is not null)
        {
            sections.Add(new ExtractedSection(
                currentHeading,
                currentLevel,
                currentStartPage,
                string.Join(" ", sectionContentParts)));
        }

        var pages = pageTexts
            .OrderBy(kv => kv.Key)
            .Select(kv => new ExtractedPage(kv.Key, string.Join("\n", kv.Value)))
            .ToList();

        if (pages.Count == 0)
        {
            pages.Add(new ExtractedPage(1, string.Join("\n", fullTextParts)));
        }

        var tables = ExtractTables(body, pages.Count);
        var fullText = string.Join("\n", fullTextParts);

        var coreProps = wordDoc.PackageProperties;
        var metadata = new DocumentMetadata(
            fileName,
            contentType,
            content.CanSeek ? content.Length : 0L,
            pages.Count,
            coreProps.Title,
            coreProps.Creator);

        return Result.Success(new DocumentExtractionResult(fullText, pages, sections, tables, metadata));
    }

    private static bool HasPageBreak(Paragraph para)
        => para.Descendants<Break>().Any(b => b.Type?.Value == BreakValues.Page);

    private static int ParseHeadingLevel(string styleId)
    {
        var digits = new string(styleId.Where(char.IsDigit).ToArray());
        return int.TryParse(digits, out var level) ? level : 1;
    }

    private static List<ExtractedTable> ExtractTables(Body body, int pageCount)
    {
        var tables = new List<ExtractedTable>();
        var pageNum = 1;

        foreach (var table in body.Elements<Table>())
        {
            var rows = table.Elements<TableRow>()
                .Select(row => (IReadOnlyList<string>)row.Elements<TableCell>()
                    .Select(cell => cell.InnerText)
                    .ToList())
                .ToList();

            if (rows.Count > 0)
            {
                tables.Add(new ExtractedTable(Math.Min(pageNum, pageCount), rows));
            }

            pageNum++;
        }

        return tables;
    }
}
