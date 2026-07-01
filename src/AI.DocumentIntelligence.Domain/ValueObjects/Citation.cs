using AI.DocumentIntelligence.Domain.Common;
using AI.DocumentIntelligence.Domain.Errors;

namespace AI.DocumentIntelligence.Domain.ValueObjects;

/// <summary>
/// A traceable reference back to a source document supporting an AI statement. Every AI
/// analysis, comparison, and chat response MUST carry one or more citations (platform hard rule).
/// </summary>
/// <param name="DocumentId">Identifier of the source document.</param>
/// <param name="DocumentName">The name of the source document.</param>
/// <param name="PageNumber">The 1-based page the referenced content appears on.</param>
/// <param name="ParagraphReference">A locator for the paragraph/section within the page (e.g. <c>"¶3"</c> or a heading).</param>
/// <param name="Snippet">The supporting excerpt quoted from the source.</param>
/// <param name="ConfidenceScore">Model confidence in the citation, from 0.0 to 1.0.</param>
public sealed record Citation(
    Guid DocumentId,
    string DocumentName,
    int PageNumber,
    string ParagraphReference,
    string Snippet,
    double ConfidenceScore)
{
    /// <summary>Creates a validated <see cref="Citation"/>, enforcing the platform's citation invariants.</summary>
    public static Result<Citation> Create(
        Guid documentId,
        string documentName,
        int pageNumber,
        string paragraphReference,
        string snippet,
        double confidenceScore)
    {
        if (documentId == Guid.Empty)
        {
            return Result.Failure<Citation>(DomainErrors.Citation.InvalidDocumentId);
        }

        if (string.IsNullOrWhiteSpace(documentName))
        {
            return Result.Failure<Citation>(DomainErrors.Citation.MissingDocumentName);
        }

        if (pageNumber < 1)
        {
            return Result.Failure<Citation>(DomainErrors.Citation.InvalidPageNumber);
        }

        if (confidenceScore is < 0.0 or > 1.0)
        {
            return Result.Failure<Citation>(DomainErrors.Citation.InvalidConfidenceScore);
        }

        return Result.Success(new Citation(documentId, documentName, pageNumber, paragraphReference, snippet, confidenceScore));
    }
}
