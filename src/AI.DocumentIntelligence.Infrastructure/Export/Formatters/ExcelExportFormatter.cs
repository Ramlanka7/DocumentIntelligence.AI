using AI.DocumentIntelligence.Application.Contracts;
using AI.DocumentIntelligence.Application.Contracts.Analysis;
using AI.DocumentIntelligence.Application.Contracts.Comparison;
using AI.DocumentIntelligence.Application.Contracts.Export;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace AI.DocumentIntelligence.Infrastructure.Export.Formatters;

/// <summary>
/// Exports analysis and comparison results as XLSX (Microsoft Excel Open XML) workbooks.
/// Each logical section is placed on a separate worksheet for easy filtering and sorting.
/// Analysis sheets: Summary, Key Findings, Risks, Recommendations, Action Items, Sources.
/// Comparison sheets: Summary, Change Log, Risks, Recommendations, Sources.
/// Uses <see cref="DocumentFormat.OpenXml"/> which is already a project dependency.
/// </summary>
internal sealed class ExcelExportFormatter : IExportFormatter
{
    // CA1861 — static readonly to avoid per-call allocation of constant arrays
    private static readonly string[] s_keyFindingsHeader = { "#", "Title", "Detail", "Citations" };
    private static readonly string[] s_risksHeader = { "#", "Title", "Severity", "Description", "Citations" };
    private static readonly string[] s_recommendationsHeader = { "#", "Title", "Detail", "Citations" };
    private static readonly string[] s_actionItemsHeader = { "#", "Description", "Owner", "Citations" };
    private static readonly string[] s_differencesHeader = { "#", "Type", "Section", "Summary", "Before", "After", "Citations" };
    private static readonly string[] s_sourcesHeader = { "#", "Document Name", "Page", "Paragraph Ref", "Confidence", "Snippet" };
    private static readonly string[] s_emptySummaryRow = { string.Empty };
    private static readonly string[] s_executiveSummaryLabelRow = { "Executive Summary" };

    public ExportFormat Format => ExportFormat.Excel;

    public ExportDocumentResult FormatAnalysis(AnalysisResult result, string title)
    {
        using var ms = new MemoryStream();
        using (var workbook = SpreadsheetDocument.Create(ms, SpreadsheetDocumentType.Workbook))
        {
            var wbPart = workbook.AddWorkbookPart();
            wbPart.Workbook = new Workbook();
            var sheets = new Sheets();
            wbPart.Workbook.AppendChild(sheets);
            AddStylesPart(wbPart);

            uint sheetId = 1;
            AddSheet(wbPart, sheets, sheetId++, "Summary",
                BuildSummaryRows(title, "Analysis", result.ExecutiveSummary, result.Sources.Count));
            AddSheet(wbPart, sheets, sheetId++, "Key Findings",
                BuildKeyFindingsRows(result.KeyFindings));
            AddSheet(wbPart, sheets, sheetId++, "Risks",
                BuildRisksRows(result.Risks));
            AddSheet(wbPart, sheets, sheetId++, "Recommendations",
                BuildRecommendationsRows(result.Recommendations));
            AddSheet(wbPart, sheets, sheetId++, "Action Items",
                BuildActionItemsRows(result.ActionItems));
            AddSheet(wbPart, sheets, sheetId, "Sources",
                BuildSourcesRows(result.Sources));

            wbPart.Workbook.Save();
        }

        return new ExportDocumentResult(
            ms.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ExportFileNames.Generate(title, "analysis", "xlsx"));
    }

    public ExportDocumentResult FormatComparison(ComparisonResult result, string title)
    {
        using var ms = new MemoryStream();
        using (var workbook = SpreadsheetDocument.Create(ms, SpreadsheetDocumentType.Workbook))
        {
            var wbPart = workbook.AddWorkbookPart();
            wbPart.Workbook = new Workbook();
            var sheets = new Sheets();
            wbPart.Workbook.AppendChild(sheets);
            AddStylesPart(wbPart);

            uint sheetId = 1;
            AddSheet(wbPart, sheets, sheetId++, "Summary",
                BuildSummaryRows(title, "Comparison", result.ExecutiveOverview, result.Sources.Count));
            AddSheet(wbPart, sheets, sheetId++, "Change Log",
                BuildDifferencesRows(result.Differences));
            AddSheet(wbPart, sheets, sheetId++, "Risks",
                BuildRisksRows(result.Risks));
            AddSheet(wbPart, sheets, sheetId++, "Recommendations",
                BuildRecommendationsRows(result.Recommendations));
            AddSheet(wbPart, sheets, sheetId, "Sources",
                BuildSourcesRows(result.Sources));

            wbPart.Workbook.Save();
        }

        return new ExportDocumentResult(
            ms.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ExportFileNames.Generate(title, "comparison", "xlsx"));
    }

    // ---- sheet data builders ---------------------------------------------------------------

    private static List<string[]> BuildSummaryRows(
        string title, string type, string summary, int sourceCount)
    {
        return new List<string[]>
        {
            new[] { "Title", title },
            new[] { "Report Type", type },
            new[] { "Generated (UTC)", DateTimeOffset.UtcNow.ToString("yyyy-MM-dd HH:mm") },
            s_emptySummaryRow,
            s_executiveSummaryLabelRow,
            new[] { summary },
            s_emptySummaryRow,
            new[] { "Total Sources", sourceCount.ToString() },
        };
    }

    private static List<string[]> BuildKeyFindingsRows(IReadOnlyList<KeyFinding> findings)
    {
        var rows = new List<string[]> { s_keyFindingsHeader };
        for (var i = 0; i < findings.Count; i++)
        {
            var f = findings[i];
            rows.Add(new[] { (i + 1).ToString(), f.Title, f.Detail, FormatCitations(f.Citations) });
        }

        return rows;
    }

    private static List<string[]> BuildRisksRows(IReadOnlyList<RiskItem> risks)
    {
        var rows = new List<string[]> { s_risksHeader };
        for (var i = 0; i < risks.Count; i++)
        {
            var r = risks[i];
            rows.Add(new[] { (i + 1).ToString(), r.Title, r.Severity, r.Description, FormatCitations(r.Citations) });
        }

        return rows;
    }

    private static List<string[]> BuildRecommendationsRows(IReadOnlyList<Recommendation> recs)
    {
        var rows = new List<string[]> { s_recommendationsHeader };
        for (var i = 0; i < recs.Count; i++)
        {
            var r = recs[i];
            rows.Add(new[] { (i + 1).ToString(), r.Title, r.Detail, FormatCitations(r.Citations) });
        }

        return rows;
    }

    private static List<string[]> BuildActionItemsRows(IReadOnlyList<ActionItem> items)
    {
        var rows = new List<string[]> { s_actionItemsHeader };
        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            rows.Add(new[] { (i + 1).ToString(), item.Description, item.Owner ?? string.Empty, FormatCitations(item.Citations) });
        }

        return rows;
    }

    private static List<string[]> BuildDifferencesRows(IReadOnlyList<DocumentDifference> diffs)
    {
        var rows = new List<string[]> { s_differencesHeader };
        for (var i = 0; i < diffs.Count; i++)
        {
            var d = diffs[i];
            rows.Add(new[]
            {
                (i + 1).ToString(),
                d.Type.ToString(),
                d.Section,
                d.Summary,
                d.Before ?? string.Empty,
                d.After ?? string.Empty,
                FormatCitations(d.Citations),
            });
        }

        return rows;
    }

    private static List<string[]> BuildSourcesRows(IReadOnlyList<Citation> sources)
    {
        var rows = new List<string[]> { s_sourcesHeader };
        for (var i = 0; i < sources.Count; i++)
        {
            var s = sources[i];
            rows.Add(new[]
            {
                (i + 1).ToString(),
                s.DocumentName,
                s.PageNumber.ToString(),
                s.ParagraphReference,
                $"{s.ConfidenceScore:P0}",
                s.Snippet,
            });
        }

        return rows;
    }

    private static string FormatCitations(IReadOnlyList<Citation> citations)
    {
        if (citations.Count == 0)
        {
            return string.Empty;
        }

        return string.Join("; ", citations.Select(c =>
            $"{c.DocumentName} p.{c.PageNumber} {c.ParagraphReference} ({c.ConfidenceScore:P0})"));
    }

    // ---- OpenXml plumbing ------------------------------------------------------------------

    private static void AddSheet(
        WorkbookPart wbPart,
        Sheets sheets,
        uint sheetId,
        string name,
        List<string[]> rows)
    {
        var wsPart = wbPart.AddNewPart<WorksheetPart>();
        var sheetData = new SheetData();
        wsPart.Worksheet = new Worksheet(sheetData);

        var isFirst = true;
        foreach (var rowCells in rows)
        {
            var row = new Row();
            var styleIndex = isFirst && rowCells.Length > 0 ? 1u : 0u; // bold header row
            foreach (var cell in rowCells)
            {
                row.AppendChild(StringCell(cell, styleIndex));
            }

            sheetData.AppendChild(row);
            isFirst = false;
        }

        wsPart.Worksheet.Save();

        sheets.AppendChild(new Sheet
        {
            Id = wbPart.GetIdOfPart(wsPart),
            SheetId = sheetId,
            Name = name,
        });
    }

    private static Cell StringCell(string value, uint styleIndex)
    {
        return new Cell
        {
            CellValue = new CellValue(value),
            DataType = CellValues.String,
            StyleIndex = styleIndex,
        };
    }

    private static void AddStylesPart(WorkbookPart wbPart)
    {
        var stylesPart = wbPart.AddNewPart<WorkbookStylesPart>();
        var stylesheet = new Stylesheet();

        var fonts = new Fonts();
        fonts.AppendChild(new Font(new FontSize { Val = 11 }));
        fonts.AppendChild(new Font(new Bold(), new FontSize { Val = 11 }));
        fonts.Count = 2;
        stylesheet.AppendChild(fonts);

        var fills = new Fills();
        fills.AppendChild(new Fill(new PatternFill { PatternType = PatternValues.None }));
        fills.AppendChild(new Fill(new PatternFill { PatternType = PatternValues.Gray125 }));
        fills.Count = 2;
        stylesheet.AppendChild(fills);

        var borders = new Borders();
        borders.AppendChild(new Border(
            new LeftBorder(),
            new RightBorder(),
            new TopBorder(),
            new BottomBorder(),
            new DiagonalBorder()));
        borders.Count = 1;
        stylesheet.AppendChild(borders);

        var cellStyleXfs = new CellStyleFormats();
        cellStyleXfs.AppendChild(new CellFormat { FontId = 0, FillId = 0, BorderId = 0 });
        cellStyleXfs.Count = 1;
        stylesheet.AppendChild(cellStyleXfs);

        var cellXfs = new CellFormats();
        cellXfs.AppendChild(new CellFormat { FontId = 0, FillId = 0, BorderId = 0, FormatId = 0 });
        cellXfs.AppendChild(new CellFormat { FontId = 1, FillId = 0, BorderId = 0, FormatId = 0, ApplyFont = true });
        cellXfs.Count = 2;
        stylesheet.AppendChild(cellXfs);

        stylesPart.Stylesheet = stylesheet;
        stylesPart.Stylesheet.Save();
    }
}
