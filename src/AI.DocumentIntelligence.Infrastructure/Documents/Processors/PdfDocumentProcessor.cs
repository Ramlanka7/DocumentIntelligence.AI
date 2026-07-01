using AI.DocumentIntelligence.Application.Abstractions.Documents;
using AI.DocumentIntelligence.Application.Contracts.Documents;
using AI.DocumentIntelligence.Domain.Common;
using UglyToad.PdfPig;

namespace AI.DocumentIntelligence.Infrastructure.Documents.Processors;

/// <summary>
/// Extracts text, headings, sections, and metadata from PDF documents using PdfPig.
/// Table detection is best-effort based on word-alignment heuristics.
/// </summary>
internal sealed class PdfDocumentProcessor : IDocumentProcessor
{
    public bool CanProcess(string fileName, string contentType)
        => Path.GetExtension(fileName).Equals(".pdf", StringComparison.OrdinalIgnoreCase)
           || contentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase);

    public Task<Result<DocumentExtractionResult>> ProcessAsync(
        Stream content,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return Task.FromResult(Extract(content, fileName, contentType));
        }
        catch (Exception ex)
        {
            return Task.FromResult(
                Result.Failure<DocumentExtractionResult>(
                    Error.Failure("Document.Pdf.ProcessingFailed", $"Failed to process PDF: {ex.Message}")));
        }
    }

    private static Result<DocumentExtractionResult> Extract(Stream content, string fileName, string contentType)
    {
        using var pdfDoc = PdfDocument.Open(content);

        var pages = new List<ExtractedPage>();
        var allWordSizes = new List<double>();

        for (var i = 1; i <= pdfDoc.NumberOfPages; i++)
        {
            var page = pdfDoc.GetPage(i);
            pages.Add(new ExtractedPage(i, page.Text));

            foreach (var word in page.GetWords())
            {
                allWordSizes.Add(word.BoundingBox.Height);
            }
        }

        var medianSize = ComputeMedian(allWordSizes);
        var sections = ExtractSections(pdfDoc, medianSize);
        var tables = ExtractTables(pdfDoc);
        var fullText = string.Join("\n", pages.Select(p => p.Text));

        var info = pdfDoc.Information;
        var metadata = new DocumentMetadata(
            fileName,
            contentType,
            content.CanSeek ? content.Length : 0L,
            pdfDoc.NumberOfPages,
            string.IsNullOrWhiteSpace(info.Title) ? null : info.Title,
            string.IsNullOrWhiteSpace(info.Author) ? null : info.Author);

        return Result.Success(new DocumentExtractionResult(fullText, pages, sections, tables, metadata));
    }

    private static List<ExtractedSection> ExtractSections(PdfDocument pdfDoc, double medianSize)
    {
        var sections = new List<ExtractedSection>();
        string? currentHeading = null;
        var currentLevel = 1;
        var currentStartPage = 1;
        var contentLines = new List<string>();

        for (var i = 1; i <= pdfDoc.NumberOfPages; i++)
        {
            var page = pdfDoc.GetPage(i);
            var words = page.GetWords().ToList();

            var lineGroups = words
                .GroupBy(w => Math.Round(w.BoundingBox.Bottom, 1))
                .OrderByDescending(g => g.Key);

            foreach (var line in lineGroups)
            {
                var lineWords = line.OrderBy(w => w.BoundingBox.Left).ToList();
                var lineText = string.Join(" ", lineWords.Select(w => w.Text));
                var avgLineSize = lineWords.Average(w => w.BoundingBox.Height);

                if (medianSize > 0 && avgLineSize > medianSize * 1.2)
                {
                    if (currentHeading is not null)
                    {
                        sections.Add(new ExtractedSection(
                            currentHeading,
                            currentLevel,
                            currentStartPage,
                            string.Join(" ", contentLines)));
                        contentLines.Clear();
                    }

                    currentHeading = lineText;
                    currentLevel = avgLineSize > medianSize * 1.5 ? 1 : 2;
                    currentStartPage = i;
                }
                else if (!string.IsNullOrWhiteSpace(lineText))
                {
                    contentLines.Add(lineText);
                }
            }
        }

        if (currentHeading is not null)
        {
            sections.Add(new ExtractedSection(
                currentHeading,
                currentLevel,
                currentStartPage,
                string.Join(" ", contentLines)));
        }

        return sections;
    }

    private static List<ExtractedTable> ExtractTables(PdfDocument pdfDoc)
    {
        var tables = new List<ExtractedTable>();

        for (var i = 1; i <= pdfDoc.NumberOfPages; i++)
        {
            var page = pdfDoc.GetPage(i);
            var words = page.GetWords().ToList();
            if (words.Count < 6)
            {
                continue;
            }

            var rowGroups = words
                .GroupBy(w => Math.Round(w.BoundingBox.Bottom, 5))
                .Where(g => g.Count() >= 3)
                .OrderByDescending(g => g.Key)
                .ToList();

            if (rowGroups.Count < 3)
            {
                continue;
            }

            var rows = rowGroups
                .Select(g => (IReadOnlyList<string>)g.OrderBy(w => w.BoundingBox.Left)
                    .Select(w => w.Text)
                    .ToList())
                .ToList();

            tables.Add(new ExtractedTable(i, rows));
        }

        return tables;
    }

    private static double ComputeMedian(List<double> values)
    {
        if (values.Count == 0)
        {
            return 0;
        }

        var sorted = values.OrderBy(v => v).ToList();
        var mid = sorted.Count / 2;
        return sorted.Count % 2 == 0
            ? (sorted[mid - 1] + sorted[mid]) / 2.0
            : sorted[mid];
    }
}
