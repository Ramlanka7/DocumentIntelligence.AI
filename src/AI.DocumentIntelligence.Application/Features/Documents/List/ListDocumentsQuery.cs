using AI.DocumentIntelligence.Application.Common.Messaging;

namespace AI.DocumentIntelligence.Application.Features.Documents.List;

/// <summary>Returns a summary list of all documents belonging to the current user.</summary>
public sealed record ListDocumentsQuery : IQuery<IReadOnlyList<DocumentSummaryDto>>;
