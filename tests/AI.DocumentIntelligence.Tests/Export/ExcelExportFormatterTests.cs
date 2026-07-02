using AI.DocumentIntelligence.Application.Contracts;
using AI.DocumentIntelligence.Application.Contracts.Analysis;
using AI.DocumentIntelligence.Application.Contracts.Comparison;
using AI.DocumentIntelligence.Application.Contracts.Export;
using AI.DocumentIntelligence.Infrastructure.Export.Formatters;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using FluentAssertions;

namespace AI.DocumentIntelligence.Tests.Export;

/// <summary>
/// Verifies that ExcelExportFormatter produces valid XLSX workbooks with the expected
/// sheets and content. Uses DocumentFormat.OpenXml to inspect the generated workbook.
/// </summary>
public sealed class ExcelExportFormatterTests
{
    private static readonly Citation SampleCitation = new(
        Guid.NewGuid(), "Contract.pdf", 5, "§3.2", "Key clause text", 0.88);

    private static AnalysisResult BuildAnalysisResult() =>
        new(
            "Executive summary text.",
            [new KeyFinding("Finding A", "Finding detail.", [SampleCitation])],
            [new RiskItem("Risk B", "Risk description.", "Medium", [SampleCitation])],
            [new Recommendation("Rec C", "Recommendation detail.", [SampleCitation])],
            [new ActionItem("Action item D", "Owner E", [SampleCitation])],
            [SampleCitation]);

    private static readonly ExcelExportFormatter Formatter = new();

    [Fact]
    public void Format_PropertyReturnsExcel()
    {
        Formatter.Format.Should().Be(ExportFormat.Excel);
    }

    [Fact]
    public void FormatAnalysis_ReturnsExcelContentType()
    {
        var result = Formatter.FormatAnalysis(BuildAnalysisResult(), "Test");

        result.ContentType.Should().Be(
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
    }

    [Fact]
    public void FormatAnalysis_FileNameHasXlsxExtension()
    {
        var result = Formatter.FormatAnalysis(BuildAnalysisResult(), "My Report");

        result.FileName.Should().EndWith(".xlsx");
        result.FileName.Should().Contain("analysis");
    }

    [Fact]
    public void FormatAnalysis_ProducesValidOpenXmlWorkbook()
    {
        var result = Formatter.FormatAnalysis(BuildAnalysisResult(), "Test");

        using var ms = new MemoryStream(result.Content);
        var act = () => SpreadsheetDocument.Open(ms, false);
        act.Should().NotThrow();
    }

    [Fact]
    public void FormatAnalysis_WorkbookHasSixSheets()
    {
        var result = Formatter.FormatAnalysis(BuildAnalysisResult(), "Test");

        using var ms = new MemoryStream(result.Content);
        using var doc = SpreadsheetDocument.Open(ms, false);
        var sheets = doc.WorkbookPart!.Workbook!.Descendants<Sheet>().ToList();

        sheets.Should().HaveCount(6);
        sheets.Select(s => s.Name!.Value).Should().Contain(
            ["Summary", "Key Findings", "Risks", "Recommendations", "Action Items", "Sources"]);
    }

    [Fact]
    public void FormatComparison_WorkbookHasFiveSheets()
    {
        var compResult = new ComparisonResult(
            "Overview text.",
            [],
            [new RiskItem("R", "Desc", "Low", [SampleCitation])],
            [new Recommendation("Rec", "Detail", [SampleCitation])],
            [SampleCitation]);

        var result = Formatter.FormatComparison(compResult, "Compare");

        using var ms = new MemoryStream(result.Content);
        using var doc = SpreadsheetDocument.Open(ms, false);
        var sheets = doc.WorkbookPart!.Workbook!.Descendants<Sheet>().ToList();

        sheets.Should().HaveCount(5);
        sheets.Select(s => s.Name!.Value).Should().Contain(
            ["Summary", "Change Log", "Risks", "Recommendations", "Sources"]);
    }

    [Fact]
    public void FormatComparison_ReturnsExcelContentType()
    {
        var compResult = new ComparisonResult("Overview.", [], [], [], []);

        var result = Formatter.FormatComparison(compResult, "Compare");

        result.ContentType.Should().Be(
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        result.FileName.Should().Contain("comparison");
    }
}
