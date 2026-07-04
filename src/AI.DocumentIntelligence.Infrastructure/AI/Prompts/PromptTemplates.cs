using AI.DocumentIntelligence.Application.Contracts.Search;

namespace AI.DocumentIntelligence.Infrastructure.AI.Prompts;

/// <summary>
/// Centralized prompt templates for all AI operations. Keeping prompts here means updates to
/// instructions, JSON schema, or tone require changes in exactly one place.
/// </summary>
internal static class PromptTemplates
{
    // $$""" raw strings use {{ }} for interpolation so JSON braces need no escaping.

    private const string CitationSchema =
        """
        {
          "documentId": "<guid>",
          "documentName": "<string>",
          "pageNumber": "<int, 1-based>",
          "paragraphReference": "<string, e.g. '¶3' or section heading>",
          "snippet": "<verbatim excerpt from source, max 200 chars>",
          "confidenceScore": "<float 0.0-1.0>"
        }
        """;

    /// <summary>Builds the system + user messages for an analysis request.</summary>
    internal static (string SystemPrompt, string UserPrompt) BuildAnalysisPrompt(
        string capability,
        string? customQuestion,
        IReadOnlyList<SearchHit> context)
    {
        var system = $$"""
            You are an expert document intelligence assistant. Analyse the supplied document
            excerpts and produce a structured JSON response for the capability: "{{capability}}".

            HARD RULE — every claim MUST be supported by a citation that references the exact
            source excerpt. Do not invent citations.

            Respond ONLY with valid JSON matching this schema (no markdown, no prose outside JSON):
            {
              "executiveSummary": "<string>",
              "keyFindings": [
                {
                  "title": "<string>",
                  "detail": "<string>",
                  "citations": [{{CitationSchema}}]
                }
              ],
              "risks": [
                {
                  "title": "<string>",
                  "description": "<string>",
                  "severity": "Low|Medium|High|Critical",
                  "citations": [{{CitationSchema}}]
                }
              ],
              "recommendations": [
                {
                  "title": "<string>",
                  "detail": "<string>",
                  "citations": [{{CitationSchema}}]
                }
              ],
              "actionItems": [
                {
                  "description": "<string>",
                  "owner": "<string or null>",
                  "citations": [{{CitationSchema}}]
                }
              ],
              "sources": [{{CitationSchema}}]
            }
            """;

        var contextBlock = BuildContextBlock(context);
        var questionBlock = string.IsNullOrWhiteSpace(customQuestion)
            ? string.Empty
            : $"\n\nCustom question: {customQuestion}";

        var user = $"Analyse the following document excerpts for capability '{capability}'.{questionBlock}\n\n{contextBlock}";

        return (system, user);
    }

    /// <summary>Builds the system + user messages for a comparison request.</summary>
    internal static (string SystemPrompt, string UserPrompt) BuildComparisonPrompt(
        string comparisonType,
        string? customInstructions,
        IReadOnlyList<SearchHit> context)
    {
        var system = $$"""
            You are an expert document comparison assistant. Compare the supplied document
            excerpts using comparison type: "{{comparisonType}}".

            Identify: added content, removed content, modified content, clause changes, pricing
            differences, risk differences, and compliance differences as applicable.

            HARD RULE — every difference and claim MUST be supported by a citation. Do not
            invent citations.

            Respond ONLY with valid JSON matching this schema (no markdown, no prose outside JSON):
            {
              "executiveOverview": "<string>",
              "differences": [
                {
                  "type": "Added|Removed|Modified",
                  "section": "<string>",
                  "before": "<string or null>",
                  "after": "<string or null>",
                  "summary": "<string>",
                  "citations": [{{CitationSchema}}]
                }
              ],
              "risks": [
                {
                  "title": "<string>",
                  "description": "<string>",
                  "severity": "Low|Medium|High|Critical",
                  "citations": [{{CitationSchema}}]
                }
              ],
              "recommendations": [
                {
                  "title": "<string>",
                  "detail": "<string>",
                  "citations": [{{CitationSchema}}]
                }
              ],
              "sources": [{{CitationSchema}}]
            }
            """;

        var contextBlock = BuildContextBlock(context);
        var instructionsBlock = string.IsNullOrWhiteSpace(customInstructions)
            ? string.Empty
            : $"\n\nAdditional instructions: {customInstructions}";

        var user = $"Compare the following document excerpts using type '{comparisonType}'.{instructionsBlock}\n\n{contextBlock}";

        return (system, user);
    }

    /// <summary>Builds the system + user messages for a RAG chat request.</summary>
    internal static (string SystemPrompt, string UserPrompt) BuildChatPrompt(
        string question,
        IReadOnlyList<SearchHit> context)
    {
        var system = $$"""
            You are a document assistant that answers questions grounded exclusively in the
            provided document excerpts. Do not use knowledge outside the excerpts.

            HARD RULE — every statement MUST be supported by a citation. If the excerpts do not
            contain enough information, say so clearly and cite the closest relevant excerpt.

            Respond ONLY with valid JSON matching this schema (no markdown, no prose outside JSON):
            {
              "answer": "<string>",
              "citations": [{{CitationSchema}}]
            }
            """;

        var contextBlock = BuildContextBlock(context);
        var user = $"Question: {question}\n\n{contextBlock}";

        return (system, user);
    }

    private static string BuildContextBlock(IReadOnlyList<SearchHit> hits)
    {
        if (hits.Count == 0)
        {
            return "No document excerpts were retrieved.";
        }

        var lines = hits.Select((h, i) =>
            $"[Excerpt {i + 1}]\n" +
            $"DocumentId: {h.DocumentId}\n" +
            $"Document: {h.DocumentName}\n" +
            $"Page: {h.PageNumber}  Section: {h.ParagraphReference}\n" +
            $"Relevance: {h.Score:F3}\n" +
            $"Content: {h.Content}");

        return "DOCUMENT EXCERPTS:\n" + string.Join("\n\n", lines);
    }
}
