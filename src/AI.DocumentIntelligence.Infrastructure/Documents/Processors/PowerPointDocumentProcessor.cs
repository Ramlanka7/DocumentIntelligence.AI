using AI.DocumentIntelligence.Application.Abstractions.Documents;
using AI.DocumentIntelligence.Application.Contracts.Documents;
using AI.DocumentIntelligence.Domain.Common;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using DWParagraph = DocumentFormat.OpenXml.Drawing.Paragraph;
using DWRun = DocumentFormat.OpenXml.Drawing.Run;
using DWTable = DocumentFormat.OpenXml.Drawing.Table;
using DWTableCell = DocumentFormat.OpenXml.Drawing.TableCell;
using DWTableRow = DocumentFormat.OpenXml.Drawing.TableRow;
using DWText = DocumentFormat.OpenXml.Drawing.Text;

namespace AI.DocumentIntelligence.Infrastructure.Documents.Processors;

/// <summary>
/// Extracts text, slide titles as headings, and tables from PPTX documents.
/// Each slide is treated as one page.
/// </summary>
internal sealed class PowerPointDocumentProcessor : IDocumentProcessor
{
    private const string PptxContentType =
        "application/vnd.openxmlformats-officedocument.presentationml.presentation";

    public bool CanProcess(string fileName, string contentType)
        => Path.GetExtension(fileName).Equals(".pptx", StringComparison.OrdinalIgnoreCase)
           || contentType.Equals(PptxContentType, StringComparison.OrdinalIgnoreCase);

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
                Error.Failure("Document.Pptx.ProcessingFailed", $"Failed to process PPTX: {ex.Message}"));
        }
    }

    private static Result<DocumentExtractionResult> Extract(Stream content, string fileName, string contentType)
    {
        using var presentationDoc = PresentationDocument.Open(content, false);
        var presentationPart = presentationDoc.PresentationPart;
        if (presentationPart is null)
        {
            return Result.Failure<DocumentExtractionResult>(
                Error.Failure("Document.Pptx.NoPresentationPart", "The PPTX file has no presentation part."));
        }

        var slideParts = presentationPart.SlideParts.ToList();
        var pages = new List<ExtractedPage>();
        var sections = new List<ExtractedSection>();
        var tables = new List<ExtractedTable>();
        var allText = new List<string>();

        var slideNumber = 1;
        foreach (var slidePart in slideParts)
        {
            var slideText = new List<string>();
            string? slideTitle = null;

            var shapeTree = slidePart.Slide?.CommonSlideData?.ShapeTree;
            var shapes = shapeTree?.Elements<Shape>() ?? [];

            foreach (var shape in shapes)
            {
                var ph = shape.NonVisualShapeProperties
                    ?.ApplicationNonVisualDrawingProperties
                    ?.PlaceholderShape;

                var shapeText = ExtractShapeText(shape);
                if (string.IsNullOrWhiteSpace(shapeText))
                {
                    continue;
                }

                var phType = ph?.Type?.Value;
                var isTitle = phType == PlaceholderValues.Title || phType == PlaceholderValues.CenteredTitle;
                if (isTitle)
                {
                    slideTitle = shapeText;
                }
                else
                {
                    slideText.Add(shapeText);
                }
            }

            foreach (var graphicFrame in shapeTree?.Elements<GraphicFrame>() ?? [])
            {
                var table = graphicFrame.Graphic?.GraphicData?.GetFirstChild<DWTable>();
                if (table is null)
                {
                    continue;
                }

                var rows = table.Elements<DWTableRow>()
                    .Select(row => (IReadOnlyList<string>)row.Elements<DWTableCell>()
                        .Select(cell => string.Concat(
                            cell.TextBody?.Elements<DWParagraph>()
                                .SelectMany(p => p.Elements<DWRun>())
                                .Select(r => r.GetFirstChild<DWText>()?.Text ?? string.Empty) ?? []))
                        .ToList())
                    .ToList();

                if (rows.Count > 0)
                {
                    tables.Add(new ExtractedTable(slideNumber, rows));
                }
            }

            if (slideTitle is not null)
            {
                sections.Add(new ExtractedSection(
                    slideTitle,
                    1,
                    slideNumber,
                    string.Join(" ", slideText)));
            }

            var pageContent = slideTitle is not null
                ? slideTitle + "\n" + string.Join("\n", slideText)
                : string.Join("\n", slideText);

            pages.Add(new ExtractedPage(slideNumber, pageContent));
            allText.Add(pageContent);
            slideNumber++;
        }

        var fullText = string.Join("\n\n", allText);
        var metadata = new DocumentMetadata(
            fileName,
            contentType,
            content.CanSeek ? content.Length : 0L,
            pages.Count,
            null,
            null);

        return Result.Success(new DocumentExtractionResult(fullText, pages, sections, tables, metadata));
    }

    private static string ExtractShapeText(Shape shape)
    {
        var textBody = shape.TextBody;
        if (textBody is null)
        {
            return string.Empty;
        }

        return string.Join("\n", textBody.Elements<DWParagraph>()
            .Select(p => string.Concat(
                p.Elements<DWRun>().Select(r => r.GetFirstChild<DWText>()?.Text ?? string.Empty))));
    }
}
