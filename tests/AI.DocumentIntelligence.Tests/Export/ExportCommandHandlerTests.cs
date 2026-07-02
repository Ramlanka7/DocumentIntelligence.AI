using AI.DocumentIntelligence.Application;
using AI.DocumentIntelligence.Application.Abstractions.Export;
using AI.DocumentIntelligence.Application.Contracts;
using AI.DocumentIntelligence.Application.Contracts.Analysis;
using AI.DocumentIntelligence.Application.Contracts.Comparison;
using AI.DocumentIntelligence.Application.Contracts.Export;
using AI.DocumentIntelligence.Application.Features.Export.ExportAnalysis;
using AI.DocumentIntelligence.Application.Features.Export.ExportComparison;
using AI.DocumentIntelligence.Domain.Common;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace AI.DocumentIntelligence.Tests.Export;

/// <summary>
/// Exercises the full MediatR pipeline (validation + handler) for export commands with a mocked
/// IExportService. Confirms the handler delegates to the service and returns its result unchanged.
/// </summary>
public sealed class ExportCommandHandlerTests
{
    private static readonly Citation SampleCitation = new(
        Guid.NewGuid(), "Doc.pdf", 1, "§1", "Excerpt", 0.95);

    private static AnalysisResult BuildAnalysisResult() =>
        new("Summary.", [], [], [], [], [SampleCitation]);

    private static ComparisonResult BuildComparisonResult() =>
        new("Overview.", [], [], [], [SampleCitation]);

    private static readonly ExportDocumentResult FakeDocument =
        new(new byte[] { 0x25, 0x50, 0x44, 0x46 }, "application/pdf", "report-analysis.pdf");

    private static ISender BuildSender(IExportService exportService)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        services.AddSingleton(exportService);
        return services.BuildServiceProvider().GetRequiredService<ISender>();
    }

    // ---- analysis handler -----------------------------------------------------------------

    [Fact]
    public async Task ExportAnalysisCommand_ValidCommand_DelegatesToExportServiceAndReturnsSuccess()
    {
        var mockService = new Mock<IExportService>();
        mockService
            .Setup(s => s.ExportAnalysisAsync(
                It.IsAny<AnalysisResult>(),
                ExportFormat.Pdf,
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(FakeDocument));

        var sender = BuildSender(mockService.Object);
        var cmd = new ExportAnalysisCommand(BuildAnalysisResult(), ExportFormat.Pdf, "My Title");

        var result = await sender.Send(cmd);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(FakeDocument);
        mockService.Verify(
            s => s.ExportAnalysisAsync(
                It.IsAny<AnalysisResult>(),
                ExportFormat.Pdf,
                "My Title",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExportAnalysisCommand_WhenServiceReturnsFailure_HandlerPropagatesFailure()
    {
        var error = Error.Failure("Export.Failed", "Something went wrong");
        var mockService = new Mock<IExportService>();
        mockService
            .Setup(s => s.ExportAnalysisAsync(
                It.IsAny<AnalysisResult>(),
                It.IsAny<ExportFormat>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<ExportDocumentResult>(error));

        var sender = BuildSender(mockService.Object);
        var cmd = new ExportAnalysisCommand(BuildAnalysisResult(), ExportFormat.Excel);

        var result = await sender.Send(cmd);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Export.Failed");
    }

    // ---- comparison handler ---------------------------------------------------------------

    [Fact]
    public async Task ExportComparisonCommand_ValidCommand_DelegatesToExportServiceAndReturnsSuccess()
    {
        var fakeDoc = new ExportDocumentResult(
            new byte[] { 0x50, 0x4B }, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "report-comparison.xlsx");

        var mockService = new Mock<IExportService>();
        mockService
            .Setup(s => s.ExportComparisonAsync(
                It.IsAny<ComparisonResult>(),
                ExportFormat.Excel,
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(fakeDoc));

        var sender = BuildSender(mockService.Object);
        var cmd = new ExportComparisonCommand(BuildComparisonResult(), ExportFormat.Excel, "Comparison Title");

        var result = await sender.Send(cmd);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(fakeDoc);
    }

    [Fact]
    public async Task ExportComparisonCommand_WhenServiceReturnsFailure_HandlerPropagatesFailure()
    {
        var mockService = new Mock<IExportService>();
        mockService
            .Setup(s => s.ExportComparisonAsync(
                It.IsAny<ComparisonResult>(),
                It.IsAny<ExportFormat>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<ExportDocumentResult>(
                Error.Validation("Export.UnsupportedFormat", "Format not supported")));

        var sender = BuildSender(mockService.Object);
        var cmd = new ExportComparisonCommand(BuildComparisonResult(), ExportFormat.Markdown);

        var result = await sender.Send(cmd);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Export.UnsupportedFormat");
    }
}
