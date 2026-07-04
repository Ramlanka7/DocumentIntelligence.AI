using AI.DocumentIntelligence.Application.Abstractions.Export;
using AI.DocumentIntelligence.Application.Contracts;
using AI.DocumentIntelligence.Application.Contracts.Analysis;
using AI.DocumentIntelligence.Application.Contracts.Comparison;
using AI.DocumentIntelligence.Application.Contracts.Export;
using AI.DocumentIntelligence.Domain.Common;
using AI.DocumentIntelligence.Infrastructure.Export;
using AI.DocumentIntelligence.Infrastructure.Export.Formatters;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace AI.DocumentIntelligence.Tests.Export;

/// <summary>
/// Verifies ExportService dispatch logic: correct formatter is resolved, CPU-bound work is
/// offloaded, failures are wrapped in Result, and null inputs produce validation errors.
/// </summary>
public sealed class ExportServiceTests
{
    private static readonly Citation SampleCitation = new(
        Guid.NewGuid(), "Doc.pdf", 1, "§1", "Snippet", 0.9);

    private static AnalysisResult BuildAnalysisResult() =>
        new("Summary", [], [], [], [], [SampleCitation]);

    private static ComparisonResult BuildComparisonResult() =>
        new("Overview", [], [], [], [SampleCitation]);

    private static IExportService BuildService()
    {
        // Register all four real formatters so we exercise the full dispatch table.
        var formatters = new IExportFormatter[]
        {
            new MarkdownExportFormatter(),
            new ExcelExportFormatter(),
            new WordExportFormatter(),
            new PdfExportFormatter(),
        };
        return new ExportService(formatters, NullLogger<ExportService>.Instance);
    }

    // ---- analysis -------------------------------------------------------------------------

    [Theory]
    [InlineData(ExportFormat.Markdown)]
    [InlineData(ExportFormat.Excel)]
    [InlineData(ExportFormat.Word)]
    [InlineData(ExportFormat.Pdf)]
    public async Task ExportAnalysisAsync_AllFormats_ReturnSuccess(ExportFormat format)
    {
        var sut = BuildService();

        var result = await sut.ExportAnalysisAsync(BuildAnalysisResult(), format);

        result.IsSuccess.Should().BeTrue();
        result.Value.Content.Should().NotBeEmpty();
        result.Value.ContentType.Should().NotBeNullOrWhiteSpace();
        result.Value.FileName.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task ExportAnalysisAsync_NullResult_ReturnsValidationFailure()
    {
        var sut = BuildService();

        var result = await sut.ExportAnalysisAsync(null!, ExportFormat.Markdown);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Export.EmptyResult");
    }

    [Fact]
    public async Task ExportAnalysisAsync_UnsupportedFormat_ReturnsValidationFailure()
    {
        // Build a service with no formatters so every format is unsupported.
        var sut = new ExportService([], NullLogger<ExportService>.Instance);

        var result = await sut.ExportAnalysisAsync(BuildAnalysisResult(), ExportFormat.Pdf);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Export.UnsupportedFormat");
    }

    [Fact]
    public async Task ExportAnalysisAsync_WithCustomTitle_ReflectedInFileName()
    {
        var sut = BuildService();

        var result = await sut.ExportAnalysisAsync(
            BuildAnalysisResult(), ExportFormat.Markdown, "My Custom Title");

        result.IsSuccess.Should().BeTrue();
        result.Value.FileName.Should().Contain("my-custom-title");
    }

    [Fact]
    public async Task ExportAnalysisAsync_NullTitle_FallsBackToDefault()
    {
        var sut = BuildService();

        var result = await sut.ExportAnalysisAsync(
            BuildAnalysisResult(), ExportFormat.Markdown, null);

        result.IsSuccess.Should().BeTrue();
        result.Value.FileName.Should().NotBeNullOrWhiteSpace();
    }

    // ---- comparison -----------------------------------------------------------------------

    [Theory]
    [InlineData(ExportFormat.Markdown)]
    [InlineData(ExportFormat.Excel)]
    [InlineData(ExportFormat.Word)]
    [InlineData(ExportFormat.Pdf)]
    public async Task ExportComparisonAsync_AllFormats_ReturnSuccess(ExportFormat format)
    {
        var sut = BuildService();

        var result = await sut.ExportComparisonAsync(BuildComparisonResult(), format);

        result.IsSuccess.Should().BeTrue();
        result.Value.Content.Should().NotBeEmpty();
        result.Value.ContentType.Should().NotBeNullOrWhiteSpace();
        result.Value.FileName.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task ExportComparisonAsync_NullResult_ReturnsValidationFailure()
    {
        var sut = BuildService();

        var result = await sut.ExportComparisonAsync(null!, ExportFormat.Markdown);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Export.EmptyResult");
    }

    [Fact]
    public async Task ExportComparisonAsync_UnsupportedFormat_ReturnsValidationFailure()
    {
        var sut = new ExportService([], NullLogger<ExportService>.Instance);

        var result = await sut.ExportComparisonAsync(BuildComparisonResult(), ExportFormat.Pdf);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Export.UnsupportedFormat");
    }

    // ---- formatter wrapping ---------------------------------------------------------------

    [Fact]
    public async Task ExportAnalysisAsync_WhenFormatterThrows_ReturnsExportFailedError()
    {
        // Use a hand-written stub because Moq cannot proxy internal types without an
        // additional [InternalsVisibleTo("DynamicProxyGenAssembly2")] attribute.
        var throwingFormatter = new ThrowingPdfFormatter();

        var sut = new ExportService(
            [throwingFormatter],
            NullLogger<ExportService>.Instance);

        var result = await sut.ExportAnalysisAsync(BuildAnalysisResult(), ExportFormat.Pdf);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Export.Failed");
        result.Error.Description.Should().Be("Export generation failed: An unexpected error occurred during export generation.");
    }

    // ---- private stub ---------------------------------------------------------------------

    /// <summary>
    /// Minimal IExportFormatter stub that always throws to exercise the ExportService
    /// exception-wrapping path without requiring Moq proxy access to internal members.
    /// </summary>
    private sealed class ThrowingPdfFormatter : IExportFormatter
    {
        public ExportFormat Format => ExportFormat.Pdf;

        public ExportDocumentResult FormatAnalysis(AnalysisResult result, string title) =>
            throw new InvalidOperationException("formatter exploded");

        public ExportDocumentResult FormatComparison(ComparisonResult result, string title) =>
            throw new InvalidOperationException("formatter exploded");
    }
}
