using AI.DocumentIntelligence.Infrastructure.Export;
using FluentAssertions;

namespace AI.DocumentIntelligence.Tests.Export;

/// <summary>
/// Verifies that ExportFileNames.Generate produces sanitized, URL-safe filenames.
/// </summary>
public sealed class ExportFileNamesTests
{
    [Fact]
    public void Generate_NormalTitle_ProducesSluggedFilename()
    {
        var result = ExportFileNames.Generate("My Contract 2024", "analysis", "pdf");

        result.Should().Be("my-contract-2024-analysis.pdf");
    }

    [Fact]
    public void Generate_NullOrEmptyTitle_FallsBackToExportPrefix()
    {
        var resultNull = ExportFileNames.Generate(string.Empty, "comparison", "xlsx");
        var resultWhitespace = ExportFileNames.Generate("   ", "comparison", "xlsx");

        resultNull.Should().Be("export-comparison.xlsx");
        resultWhitespace.Should().Be("export-comparison.xlsx");
    }

    [Fact]
    public void Generate_SpecialCharactersInTitle_AreRemoved()
    {
        var result = ExportFileNames.Generate("Report: Q1/2024!", "analysis", "md");

        result.Should().Be("report-q12024-analysis.md");
    }

    [Fact]
    public void Generate_MultipleConsecutiveSpaces_CollapsedToSingleDash()
    {
        var result = ExportFileNames.Generate("A  B   C", "analysis", "docx");

        result.Should().Be("a-b-c-analysis.docx");
    }

    [Fact]
    public void Generate_TitleLongerThan80Chars_IsTruncatedAt80()
    {
        var longTitle = new string('a', 90);

        var result = ExportFileNames.Generate(longTitle, "analysis", "pdf");

        // slug is 90 'a' chars, truncated to 80; then "-analysis.pdf"
        result.Should().StartWith(new string('a', 80));
        result.Should().EndWith("-analysis.pdf");
    }
}
