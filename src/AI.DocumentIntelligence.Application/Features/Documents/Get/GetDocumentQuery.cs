using AI.DocumentIntelligence.Application.Common.Messaging;

namespace AI.DocumentIntelligence.Application.Features.Documents.Queries;

/// <summary>Returns the full detail view of a single document by its identifier.</summary>
/// <param name="Id">The document's unique identifier.</param>
public sealed record GetDocumentQuery(Guid Id) : IQuery<DocumentDetailDto>;
