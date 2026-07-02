using AI.DocumentIntelligence.Application;
using AI.DocumentIntelligence.Application.Contracts;
using AI.DocumentIntelligence.Application.Contracts.Analysis;
using AI.DocumentIntelligence.Application.Contracts.Export;
using AI.DocumentIntelligence.Application.Features.Export.ExportAnalysis;
using AI.DocumentIntelligence.Application.Features.Export.ExportComparison;
using AI.DocumentIntelligence.Application.Contracts.Comparison;
using AI.DocumentIntelligence.Domain.Common;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AI.DocumentIntelligence.Tests.Export;

/// <summary>
/// Verifies FluentValidation pipeline behaviours for the export commands. The MediatR
/// pipeline registered by AddApplication() must short-circuit with a ValidationError before
/// the handler is reached when input is invalid.
/// </summary>
public sealed class ExportCommandValidatorTests
{
    private static ISender BuildSender()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        return services.BuildServiceProvider().GetRequiredService<ISender>();
    }

    private static AnalysisResult BuildAnalysisResult() =>
        new("Summary", [], [], [], [], []);

    private static ComparisonResult BuildComparisonResult() =>
        new("Overview", [], [], [], []);

    // ---- ExportAnalysisCommand ------------------------------------------------------------

    [Fact]
    public async Task ExportAnalysisCommand_NullResult_IsRejectedByValidation()
    {
        var sender = BuildSender();

        // Null AnalysisResult — validator must catch this.
        var result = await sender.Send(
            new ExportAnalysisCommand(null!, ExportFormat.Markdown));

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public async Task ExportAnalysisCommand_InvalidFormatValue_IsRejectedByValidation()
    {
        var sender = BuildSender();

        // Cast 99 to ExportFormat to simulate an out-of-range value.
        var result = await sender.Send(
            new ExportAnalysisCommand(BuildAnalysisResult(), (ExportFormat)99));

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    // ---- ExportComparisonCommand ----------------------------------------------------------

    [Fact]
    public async Task ExportComparisonCommand_NullResult_IsRejectedByValidation()
    {
        var sender = BuildSender();

        var result = await sender.Send(
            new ExportComparisonCommand(null!, ExportFormat.Pdf));

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public async Task ExportComparisonCommand_InvalidFormatValue_IsRejectedByValidation()
    {
        var sender = BuildSender();

        var result = await sender.Send(
            new ExportComparisonCommand(BuildComparisonResult(), (ExportFormat)(-1)));

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
    }
}
