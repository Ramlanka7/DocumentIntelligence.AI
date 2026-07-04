namespace AI.DocumentIntelligence.Infrastructure.AI.Services;

// Internal DTOs used solely to deserialize the structured JSON that AI providers return.
// They are mapped to the Application-layer contract types inside each service.

internal sealed record JsonCitationDto(
    string DocumentId,
    string DocumentName,
    int PageNumber,
    string ParagraphReference,
    string Snippet,
    double ConfidenceScore);

internal sealed record JsonKeyFindingDto(
    string Title,
    string Detail,
    List<JsonCitationDto> Citations);

internal sealed record JsonRiskItemDto(
    string Title,
    string Description,
    string Severity,
    List<JsonCitationDto> Citations);

internal sealed record JsonRecommendationDto(
    string Title,
    string Detail,
    List<JsonCitationDto> Citations);

internal sealed record JsonActionItemDto(
    string Description,
    string? Owner,
    List<JsonCitationDto> Citations);

internal sealed record JsonDocumentDifferenceDto(
    string Type,
    string Section,
    string? Before,
    string? After,
    string Summary,
    List<JsonCitationDto> Citations);

internal sealed record JsonAnalysisResultDto(
    string ExecutiveSummary,
    List<JsonKeyFindingDto> KeyFindings,
    List<JsonRiskItemDto> Risks,
    List<JsonRecommendationDto> Recommendations,
    List<JsonActionItemDto> ActionItems,
    List<JsonCitationDto> Sources);

internal sealed record JsonComparisonResultDto(
    string ExecutiveOverview,
    List<JsonDocumentDifferenceDto> Differences,
    List<JsonRiskItemDto> Risks,
    List<JsonRecommendationDto> Recommendations,
    List<JsonCitationDto> Sources);

internal sealed record JsonChatResponseDto(
    string Answer,
    List<JsonCitationDto> Citations);
