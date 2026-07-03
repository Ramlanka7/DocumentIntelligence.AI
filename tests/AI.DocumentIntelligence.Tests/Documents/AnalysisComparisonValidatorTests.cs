using AI.DocumentIntelligence.Application.Features.Analysis;
using AI.DocumentIntelligence.Application.Features.Chat;
using AI.DocumentIntelligence.Application.Features.Comparison;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace AI.DocumentIntelligence.Tests.Documents;

/// <summary>
/// Validator tests for AnalyzeDocumentsCommand, CompareDocumentsCommand and ChatCommand.
/// Covers the critical-path validation for analysis and comparison.
/// </summary>
public sealed class AnalysisComparisonValidatorTests
{
    // ---- AnalyzeDocumentsCommandValidator ----

    [Fact]
    public void AnalysisValidator_ValidCommand_NoErrors()
    {
        var validator = new AnalyzeDocumentsCommandValidator();
        var cmd = new AnalyzeDocumentsCommand([Guid.NewGuid()], "ExecutiveSummary", null);

        validator.TestValidate(cmd).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void AnalysisValidator_NoDocuments_HasError()
    {
        var validator = new AnalyzeDocumentsCommandValidator();
        var cmd = new AnalyzeDocumentsCommand([], "ExecutiveSummary", null);

        validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.DocumentIds);
    }

    [Fact]
    public void AnalysisValidator_TooManyDocuments_HasError()
    {
        var validator = new AnalyzeDocumentsCommandValidator();
        var ids = Enumerable.Range(0, 5).Select(_ => Guid.NewGuid()).ToList();
        var cmd = new AnalyzeDocumentsCommand(ids, "ExecutiveSummary", null);

        validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.DocumentIds);
    }

    [Fact]
    public void AnalysisValidator_EmptyCapability_HasError()
    {
        var validator = new AnalyzeDocumentsCommandValidator();
        var cmd = new AnalyzeDocumentsCommand([Guid.NewGuid()], "", null);

        validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.Capability);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(4)]
    public void AnalysisValidator_ValidDocumentCounts_NoErrors(int count)
    {
        var validator = new AnalyzeDocumentsCommandValidator();
        var ids = Enumerable.Range(0, count).Select(_ => Guid.NewGuid()).ToList();
        var cmd = new AnalyzeDocumentsCommand(ids, "KeyInsights", null);

        validator.TestValidate(cmd).ShouldNotHaveAnyValidationErrors();
    }

    // ---- CompareDocumentsCommandValidator ----

    [Fact]
    public void ComparisonValidator_ValidCommand_NoErrors()
    {
        var validator = new CompareDocumentsCommandValidator();
        var cmd = new CompareDocumentsCommand(
            [Guid.NewGuid(), Guid.NewGuid()], "StructuredDiff", null);

        validator.TestValidate(cmd).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ComparisonValidator_OneDocument_HasError()
    {
        var validator = new CompareDocumentsCommandValidator();
        var cmd = new CompareDocumentsCommand([Guid.NewGuid()], "StructuredDiff", null);

        validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.DocumentIds);
    }

    [Fact]
    public void ComparisonValidator_TooManyDocuments_HasError()
    {
        var validator = new CompareDocumentsCommandValidator();
        var ids = Enumerable.Range(0, 5).Select(_ => Guid.NewGuid()).ToList();
        var cmd = new CompareDocumentsCommand(ids, "StructuredDiff", null);

        validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.DocumentIds);
    }

    [Fact]
    public void ComparisonValidator_EmptyType_HasError()
    {
        var validator = new CompareDocumentsCommandValidator();
        var cmd = new CompareDocumentsCommand(
            [Guid.NewGuid(), Guid.NewGuid()], "", null);

        validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.ComparisonType);
    }

    // ---- ChatCommandValidator ----

    [Fact]
    public void ChatValidator_ValidCommand_NoErrors()
    {
        var validator = new ChatCommandValidator();
        var cmd = new ChatCommand(
            Guid.NewGuid(),
            [Guid.NewGuid()],
            "What are the key findings?",
            []);

        validator.TestValidate(cmd).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ChatValidator_EmptyMessage_HasError()
    {
        var validator = new ChatCommandValidator();
        var cmd = new ChatCommand(Guid.NewGuid(), [Guid.NewGuid()], "", []);

        validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.Message);
    }

    [Fact]
    public void ChatValidator_NoDocuments_HasError()
    {
        var validator = new ChatCommandValidator();
        var cmd = new ChatCommand(Guid.NewGuid(), [], "Hello?", []);

        validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.DocumentIds);
    }
}
