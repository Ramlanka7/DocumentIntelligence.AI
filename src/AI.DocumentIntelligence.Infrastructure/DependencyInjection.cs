using AI.DocumentIntelligence.Application.Abstractions.Documents;
using AI.DocumentIntelligence.Infrastructure.Documents;
using AI.DocumentIntelligence.Infrastructure.Documents.Processors;
using Microsoft.Extensions.DependencyInjection;

namespace AI.DocumentIntelligence.Infrastructure;

/// <summary>
/// Wires all Infrastructure services into the DI container. Called once at API startup.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddTransient<IDocumentProcessor, PdfDocumentProcessor>();
        services.AddTransient<IDocumentProcessor, WordDocumentProcessor>();
        services.AddTransient<IDocumentProcessor, TextDocumentProcessor>();
        services.AddTransient<IDocumentProcessor, CsvDocumentProcessor>();
        services.AddTransient<IDocumentProcessor, PowerPointDocumentProcessor>();

        services.AddTransient<IDocumentProcessorFactory, DocumentProcessorFactory>();

        return services;
    }
}
