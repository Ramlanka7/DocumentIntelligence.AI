using AI.DocumentIntelligence.Application.Abstractions.Documents;
using AI.DocumentIntelligence.Domain.Common;

namespace AI.DocumentIntelligence.Infrastructure.Documents;

/// <summary>
/// Selects the first registered <see cref="IDocumentProcessor"/> that declares it can handle the
/// supplied file name and content type. New formats drop in by registering an additional
/// <see cref="IDocumentProcessor"/> — no changes to this class required (Open/Closed).
/// </summary>
internal sealed class DocumentProcessorFactory : IDocumentProcessorFactory
{
    private readonly IEnumerable<IDocumentProcessor> _processors;

    public DocumentProcessorFactory(IEnumerable<IDocumentProcessor> processors)
        => _processors = processors;

    public Result<IDocumentProcessor> Resolve(string fileName, string contentType)
    {
        var processor = _processors.FirstOrDefault(p => p.CanProcess(fileName, contentType));

        return processor is null
            ? Result.Failure<IDocumentProcessor>(
                Error.NotFound("Document.Processor.NotFound",
                    $"No processor found for '{fileName}' ({contentType})."))
            : Result.Success(processor);
    }
}
