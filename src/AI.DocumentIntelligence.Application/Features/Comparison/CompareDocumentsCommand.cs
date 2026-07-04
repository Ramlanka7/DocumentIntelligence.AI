using AI.DocumentIntelligence.Application.Common.Messaging;
using AI.DocumentIntelligence.Application.Contracts.Comparison;

namespace AI.DocumentIntelligence.Application.Features.Comparison;

/// <summary>
/// Triggers an AI-powered comparison of two to four documents.
/// </summary>
/// <param name="DocumentIds">The documents to compare (2–4).</param>
/// <param name="ComparisonType">The comparison type (e.g. "SideBySide", "Version", "Contract").</param>
/// <param name="CustomInstructions">Optional free-text instructions for a custom comparison.</param>
public sealed record CompareDocumentsCommand(
    IReadOnlyList<Guid> DocumentIds,
    string ComparisonType,
    string? CustomInstructions) : ICommand<ComparisonResult>;
