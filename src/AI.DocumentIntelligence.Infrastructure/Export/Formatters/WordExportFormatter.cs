using AI.DocumentIntelligence.Application.Contracts;
using AI.DocumentIntelligence.Application.Contracts.Analysis;
using AI.DocumentIntelligence.Application.Contracts.Comparison;
using AI.DocumentIntelligence.Application.Contracts.Export;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace AI.DocumentIntelligence.Infrastructure.Export.Formatters;

/// <summary>
/// Exports analysis and comparison results as DOCX (Microsoft Word Open XML) documents.
/// Structure: Title → Executive Summary → Findings/Differences → Risks → Recommendations
/// → Action Items / Change Log → Sources. Every section item includes its citations.
/// Uses <see cref="DocumentFormat.OpenXml"/> which is already a project dependency.
/// </summary>
internal sealed class WordExportFormatter : IExportFormatter
{
    public ExportFormat Format => ExportFormat.Word;

    public ExportDocumentResult FormatAnalysis(AnalysisResult result, string title)
    {
        using var ms = new MemoryStream();
        using (var doc = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document, true))
        {
            var mainPart = doc.AddMainDocumentPart();
            mainPart.Document = new Document();
            var body = new Body();
            mainPart.Document.Append(body);
            AddStylesPart(mainPart);

            AppendTitle(body, title);
            AppendSubtitle(body, "Document Intelligence Analysis Report");

            AppendH1(body, "Executive Summary");
            AppendBody(body, result.ExecutiveSummary);

            if (result.KeyFindings.Count > 0)
            {
                AppendH1(body, "Key Findings");
                foreach (var finding in result.KeyFindings)
                {
                    AppendH2(body, finding.Title);
                    AppendBody(body, finding.Detail);
                    AppendCitations(body, finding.Citations);
                }
            }

            if (result.Risks.Count > 0)
            {
                AppendH1(body, "Risks");
                foreach (var risk in result.Risks)
                {
                    AppendH2(body, $"{risk.Title}  [Severity: {risk.Severity}]");
                    AppendBody(body, risk.Description);
                    AppendCitations(body, risk.Citations);
                }
            }

            if (result.Recommendations.Count > 0)
            {
                AppendH1(body, "Recommendations");
                foreach (var rec in result.Recommendations)
                {
                    AppendH2(body, rec.Title);
                    AppendBody(body, rec.Detail);
                    AppendCitations(body, rec.Citations);
                }
            }

            if (result.ActionItems.Count > 0)
            {
                AppendH1(body, "Action Items");
                foreach (var item in result.ActionItems)
                {
                    var ownerSuffix = string.IsNullOrWhiteSpace(item.Owner)
                        ? string.Empty
                        : $"  (Owner: {item.Owner})";
                    AppendBullet(body, item.Description + ownerSuffix);
                    AppendCitations(body, item.Citations);
                }
            }

            if (result.Sources.Count > 0)
            {
                AppendHorizontalRule(body);
                AppendH1(body, "Sources");
                for (var i = 0; i < result.Sources.Count; i++)
                {
                    AppendCitationLine(body, i + 1, result.Sources[i]);
                }
            }

            mainPart.Document.Save();
        }

        return new ExportDocumentResult(
            ms.ToArray(),
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ExportFileNames.Generate(title, "analysis", "docx"));
    }

    public ExportDocumentResult FormatComparison(ComparisonResult result, string title)
    {
        using var ms = new MemoryStream();
        using (var doc = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document, true))
        {
            var mainPart = doc.AddMainDocumentPart();
            mainPart.Document = new Document();
            var body = new Body();
            mainPart.Document.Append(body);
            AddStylesPart(mainPart);

            AppendTitle(body, title);
            AppendSubtitle(body, "Document Intelligence Comparison Report");

            AppendH1(body, "Executive Overview");
            AppendBody(body, result.ExecutiveOverview);

            if (result.Differences.Count > 0)
            {
                AppendH1(body, "Change Log");
                foreach (var diff in result.Differences)
                {
                    AppendH2(body, $"[{diff.Type}] {diff.Section}");
                    AppendBody(body, diff.Summary);
                    if (!string.IsNullOrWhiteSpace(diff.Before))
                    {
                        AppendLabeledText(body, "Before", diff.Before);
                    }

                    if (!string.IsNullOrWhiteSpace(diff.After))
                    {
                        AppendLabeledText(body, "After", diff.After);
                    }

                    AppendCitations(body, diff.Citations);
                }
            }

            if (result.Risks.Count > 0)
            {
                AppendH1(body, "Risks");
                foreach (var risk in result.Risks)
                {
                    AppendH2(body, $"{risk.Title}  [Severity: {risk.Severity}]");
                    AppendBody(body, risk.Description);
                    AppendCitations(body, risk.Citations);
                }
            }

            if (result.Recommendations.Count > 0)
            {
                AppendH1(body, "Recommendations");
                foreach (var rec in result.Recommendations)
                {
                    AppendH2(body, rec.Title);
                    AppendBody(body, rec.Detail);
                    AppendCitations(body, rec.Citations);
                }
            }

            if (result.Sources.Count > 0)
            {
                AppendHorizontalRule(body);
                AppendH1(body, "Sources");
                for (var i = 0; i < result.Sources.Count; i++)
                {
                    AppendCitationLine(body, i + 1, result.Sources[i]);
                }
            }

            mainPart.Document.Save();
        }

        return new ExportDocumentResult(
            ms.ToArray(),
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ExportFileNames.Generate(title, "comparison", "docx"));
    }

    // ---- paragraph builders ----------------------------------------------------------------

    private static void AppendTitle(Body body, string text) =>
        body.AppendChild(MakeParagraph(text, "Title"));

    private static void AppendSubtitle(Body body, string text) =>
        body.AppendChild(MakeParagraph(text, "Subtitle"));

    private static void AppendH1(Body body, string text) =>
        body.AppendChild(MakeParagraph(text, "Heading1"));

    private static void AppendH2(Body body, string text) =>
        body.AppendChild(MakeParagraph(text, "Heading2"));

    private static void AppendBody(Body body, string text)
    {
        if (!string.IsNullOrWhiteSpace(text))
        {
            body.AppendChild(MakeParagraph(text, null));
        }
    }

    private static void AppendBullet(Body body, string text)
    {
        var para = new Paragraph();
        var ppr = new ParagraphProperties();
        ppr.AppendChild(new ParagraphStyleId { Val = "ListParagraph" });
        ppr.AppendChild(new NumberingProperties(
            new NumberingLevelReference { Val = 0 },
            new NumberingId { Val = 1 }));
        para.AppendChild(ppr);
        para.AppendChild(new Run(new Text(text) { Space = SpaceProcessingModeValues.Preserve }));
        body.AppendChild(para);
    }

    private static void AppendLabeledText(Body body, string label, string text)
    {
        var para = new Paragraph();
        para.AppendChild(new Run(
            new RunProperties(new Bold()),
            new Text(label + ": ") { Space = SpaceProcessingModeValues.Preserve }));
        para.AppendChild(new Run(new Text(text) { Space = SpaceProcessingModeValues.Preserve }));
        body.AppendChild(para);
    }

    private static void AppendCitations(Body body, IReadOnlyList<Citation> citations)
    {
        if (citations.Count == 0)
        {
            return;
        }

        foreach (var c in citations)
        {
            var para = new Paragraph();
            var rpr = new RunProperties(
                new Italic(),
                new FontSize { Val = "18" },     // 9 pt = 18 half-points
                new Color { Val = "666666" });
            var text = $"  [{c.DocumentName}, p.{c.PageNumber}, {c.ParagraphReference}" +
                       $" — confidence: {c.ConfidenceScore:P0}]  \"{c.Snippet}\"";
            para.AppendChild(new Run(rpr, new Text(text) { Space = SpaceProcessingModeValues.Preserve }));
            body.AppendChild(para);
        }
    }

    private static void AppendCitationLine(Body body, int index, Citation c)
    {
        var text = $"[{index}] {c.DocumentName}, p.{c.PageNumber}, {c.ParagraphReference}" +
                   $" — confidence: {c.ConfidenceScore:P0}  \"{c.Snippet}\"";
        body.AppendChild(MakeParagraph(text, null));
    }

    private static void AppendHorizontalRule(Body body)
    {
        var para = new Paragraph();
        var ppr = new ParagraphProperties();
        var pBdr = new ParagraphBorders();
        pBdr.AppendChild(new TopBorder
        {
            Val = BorderValues.Single,
            Size = 6,
            Space = 1,
            Color = "AAAAAA",
        });
        ppr.AppendChild(pBdr);
        para.AppendChild(ppr);
        body.AppendChild(para);
    }

    private static Paragraph MakeParagraph(string text, string? styleId)
    {
        var para = new Paragraph();
        if (!string.IsNullOrEmpty(styleId))
        {
            var ppr = new ParagraphProperties();
            ppr.AppendChild(new ParagraphStyleId { Val = styleId });
            para.AppendChild(ppr);
        }

        para.AppendChild(new Run(new Text(text) { Space = SpaceProcessingModeValues.Preserve }));
        return para;
    }

    // ---- minimal styles part ---------------------------------------------------------------

    private static void AddStylesPart(MainDocumentPart mainPart)
    {
        var stylesPart = mainPart.AddNewPart<StyleDefinitionsPart>();
        stylesPart.Styles = new Styles(
            BuildStyle("Normal",        isBold: false, fontSize: "22", baseStyle: null),
            BuildStyle("Title",         isBold: true,  fontSize: "40", baseStyle: "Normal"),
            BuildStyle("Subtitle",      isBold: false, fontSize: "26", baseStyle: "Normal"),
            BuildStyle("Heading1",      isBold: true,  fontSize: "32", baseStyle: "Normal"),
            BuildStyle("Heading2",      isBold: true,  fontSize: "28", baseStyle: "Normal"),
            BuildStyle("ListParagraph", isBold: false, fontSize: "22", baseStyle: "Normal"));
        stylesPart.Styles.Save();
    }

    private static Style BuildStyle(string styleId, bool isBold, string fontSize, string? baseStyle)
    {
        var style = new Style
        {
            Type = StyleValues.Paragraph,
            StyleId = styleId,
        };
        style.AppendChild(new StyleName { Val = styleId });
        if (!string.IsNullOrEmpty(baseStyle))
        {
            style.AppendChild(new BasedOn { Val = baseStyle });
        }

        var rpr = new StyleRunProperties();
        if (isBold)
        {
            rpr.AppendChild(new Bold());
        }

        rpr.AppendChild(new FontSize { Val = fontSize });
        style.AppendChild(rpr);
        return style;
    }
}
