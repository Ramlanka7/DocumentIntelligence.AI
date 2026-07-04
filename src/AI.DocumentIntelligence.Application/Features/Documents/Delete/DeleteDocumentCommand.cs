using AI.DocumentIntelligence.Application.Common.Messaging;

namespace AI.DocumentIntelligence.Application.Features.Documents.Delete;

/// <summary>Deletes a document from storage and removes it from the repository.</summary>
/// <param name="Id">The document's unique identifier.</param>
public sealed record DeleteDocumentCommand(Guid Id) : ICommand;
