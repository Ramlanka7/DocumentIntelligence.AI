using AI.DocumentIntelligence.Domain.Common;

namespace AI.DocumentIntelligence.Application.Abstractions.Documents;

/// <summary>
/// Resolves the correct <see cref="IDocumentProcessor"/> for a given file name and content type,
/// enabling new formats to drop in without modifying any existing code (Open/Closed).
/// </summary>
public interface IDocumentProcessorFactory
{
    /// <summary>
    /// Returns the first registered processor that can handle <paramref name="fileName"/> /
    /// <paramref name="contentType"/>, or a failure result if no processor is registered for
    /// the supplied format.
    /// </summary>
    public Result<IDocumentProcessor> Resolve(string fileName, string contentType);
}
