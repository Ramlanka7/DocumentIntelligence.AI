using System.Text;
using AI.DocumentIntelligence.Application.Contracts;
using AI.DocumentIntelligence.Application.Contracts.Analysis;
using AI.DocumentIntelligence.Application.Contracts.Export;
using AI.DocumentIntelligence.Infrastructure.Export.Formatters;
using FluentAssertions;

namespace AI.DocumentIntelligence.Tests.Export;

/// <summary>
/// Verifies that MarkdownExportFormatter produces valid GitHub-flavoured Markdown
/// containing all required sections, headings, and citations.
/// </summary>
public sealed class MarkdownExportFormatterTests
{
    private static readonly Citation SampleCitation = new(
        Guid.NewGuid(), "Contract.pdf", 3, "§2.1", "Sample snippet", 0.92);

    private static AnalysisResult BuildAnalysisResult() =>
        new(
            ExecutiveSummary: "This is the executive summary.",
            KeyFindings:
            [
                new KeyFinding("Finding One", "Detail of finding one.", [SampleCitation]),
            ],
            Risks:
            [
                new RiskItem("Risk Alpha", "Description of risk alpha.", "High", [SampleCitation]),
            ],
            Recommendations:
            [
                new Recommendation("Rec One", "Detail of rec one.", [SampleCitation]),
            ],
            ActionItems:
            [
                new ActionItem("Do something urgent", "Legal", [SampleCitation]),
            ],
            Sources: [SampleCitation]);

    private static readonly MarkdownExportFormatter Formatter = new();

    [Fact]
    public void FormatAnalysis_ReturnsMarkdownContentType()
    {
        var result = Formatter.FormatAnalysis(BuildAnalysisResult(), "Test Report");

        result.ContentType.Should().StartWith("text/markdown");
    }

    [Fact]
    public void FormatAnalysis_ReturnsNonEmptyBytes()
    {
        var result = Formatter.FormatAnalysis(BuildAnalysisResult(), "Test Report");

        result.Content.Should().NotBeEmpty();
    }

    [Fact]
    public void FormatAnalysis_FileNameHasMdExtension()
    {
        var result = Formatter.FormatAnalysis(BuildAnalysisResult(), "My Report");

        result.FileName.Should().EndWith(".md");
        result.FileName.Should().Contain("analysis");
    }

    [Fact]
    public void FormatAnalysis_ContentContainsTitle()
    {
        var result = Formatter.FormatAnalysis(BuildAnalysisResult(), "My Analysis Title");
        var text = Encoding.UTF8.GetString(result.Content);

        text.Should().Contain("My Analysis Title");
    }

    [Fact]
    public void FormatAnalysis_ContentContainsExecutiveSummary()
    {
        var result = Formatter.FormatAnalysis(BuildAnalysisResult(), "T");
        var text = Encoding.UTF8.GetString(result.Content);

        text.Should().Contain("Executive Summary");
        text.Should().Contain("This is the executive summary.");
    }

    [Fact]
    public void FormatAnalysis_ContentContainsKeyFindingsSection()
    {
        var result = Formatter.FormatAnalysis(BuildAnalysisResult(), "T");
        var text = Encoding.UTF8.GetString(result.Content);

        text.Should().Contain("Key Findings");
        text.Should().Contain("Finding One");
        text.Should().Contain("Detail of finding one.");
    }

    [Fact]
    public void FormatAnalysis_ContentContainsRisksSection()
    {
        var result = Formatter.FormatAnalysis(BuildAnalysisResult(), "T");
        var text = Encoding.UTF8.GetString(result.Content);

        text.Should().Contain("Risks");
        text.Should().Contain("Risk Alpha");
        text.Should().Contain("High");
    }

    [Fact]
    public void FormatAnalysis_ContentContainsRecommendationsSection()
    {
        var result = Formatter.FormatAnalysis(BuildAnalysisResult(), "T");
        var text = Encoding.UTF8.GetString(result.Content);

        text.Should().Contain("Recommendations");
        text.Should().Contain("Rec One");
    }

    [Fact]
    public void FormatAnalysis_ContentContainsActionItemsSection()
    {
        var result = Formatter.FormatAnalysis(BuildAnalysisResult(), "T");
        var text = Encoding.UTF8.GetString(result.Content);

        text.Should().Contain("Action Items");
        text.Should().Contain("Do something urgent");
        text.Should().Contain("Legal");
    }

    [Fact]
    public void FormatAnalysis_ContentContainsCitations()
    {
        var result = Formatter.FormatAnalysis(BuildAnalysisResult(), "T");
        var text = Encoding.UTF8.GetString(result.Content);

        text.Should().Contain("Contract.pdf");
        text.Should().Contain("§2.1");
        text.Should().Contain("Sample snippet");
    }

    [Fact]
    public void FormatAnalysis_ContentContainsSourcesSection()
    {
        var result = Formatter.FormatAnalysis(BuildAnalysisResult(), "T");
        var text = Encoding.UTF8.GetString(result.Content);

        text.Should().Contain("Sources");
    }

    [Fact]
    public void FormatAnalysis_EmptySections_NotRendered()
    {
        var minimal = new AnalysisResult(
            "Just a summary.", [], [], [], [],
            []);

        var result = Formatter.FormatAnalysis(minimal, "Minimal");
        var text = Encoding.UTF8.GetString(result.Content);

        text.Should().NotContain("Key Findings");
        text.Should().NotContain("Risks");
        text.Should().NotContain("Recommendations");
        text.Should().NotContain("Action Items");
        text.Should().NotContain("Sources");
    }

    [Fact]
    public void Format_PropertyReturnsMarkdown()
    {
        Formatter.Format.Should().Be(ExportFormat.Markdown);
    }
}
