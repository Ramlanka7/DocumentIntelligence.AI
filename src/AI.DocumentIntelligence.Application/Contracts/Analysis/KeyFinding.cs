using AI.DocumentIntelligence.Application.Contracts;

namespace AI.DocumentIntelligence.Application.Contracts.Analysis;

/// <summary>A noteworthy finding surfaced during analysis, with supporting citations.</summary>
/// <param name="Title">Short headline for the finding.</param>
/// <param name="Detail">Explanation of the finding.</param>
/// <param name="Citations">Sources supporting the finding.</param>
public sealed record KeyFinding(string Title, string Detail, IReadOnlyList<Citation> Citations);
