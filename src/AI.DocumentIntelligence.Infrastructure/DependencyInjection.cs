using AI.DocumentIntelligence.Application.Abstractions;
using AI.DocumentIntelligence.Application.Abstractions.AI;
using AI.DocumentIntelligence.Application.Abstractions.Documents;
using AI.DocumentIntelligence.Application.Abstractions.Export;
using AI.DocumentIntelligence.Application.Abstractions.Identity;
using AI.DocumentIntelligence.Application.Abstractions.Search;
using AI.DocumentIntelligence.Application.Abstractions.Storage;
using AI.DocumentIntelligence.Infrastructure.AI.Embedding;
using AI.DocumentIntelligence.Infrastructure.AI.Options;
using AI.DocumentIntelligence.Infrastructure.AI.Providers;
using AI.DocumentIntelligence.Infrastructure.AI.Search;
using AI.DocumentIntelligence.Infrastructure.AI.Services;
using AI.DocumentIntelligence.Infrastructure.Auth;
using AI.DocumentIntelligence.Infrastructure.Documents;
using AI.DocumentIntelligence.Infrastructure.Documents.Chunking;
using AI.DocumentIntelligence.Infrastructure.Documents.Processors;
using AI.DocumentIntelligence.Infrastructure.Export;
using AI.DocumentIntelligence.Infrastructure.Export.Formatters;
using AI.DocumentIntelligence.Infrastructure.HealthChecks;
using AI.DocumentIntelligence.Infrastructure.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AI.DocumentIntelligence.Infrastructure;

/// <summary>
/// Wires all Infrastructure services into the DI container. Called once at API startup.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ---- Auth (T06) ----
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<UploadOptions>(configuration.GetSection(UploadOptions.SectionName));

        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddScoped<IFileUploadValidator, FileUploadValidator>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddSingleton<IFileStorage, LocalFileStorage>();

        // ---- Document processors (T04) ----
        services.AddTransient<IDocumentProcessor, PdfDocumentProcessor>();
        services.AddTransient<IDocumentProcessor, WordDocumentProcessor>();
        services.AddTransient<IDocumentProcessor, TextDocumentProcessor>();
        services.AddTransient<IDocumentProcessor, CsvDocumentProcessor>();
        services.AddTransient<IDocumentProcessor, PowerPointDocumentProcessor>();

        services.AddTransient<IDocumentProcessorFactory, DocumentProcessorFactory>();

        // ---- Chunking (T05) ----
        services.Configure<ChunkingOptions>(
            configuration.GetSection(ChunkingOptions.SectionName));

        services.AddSingleton<IChunkingService, ChunkingService>();

        // ---- Azure OpenAI options + embedding (T05) ----
        services.Configure<AzureOpenAIOptions>(
            configuration.GetSection(AzureOpenAIOptions.SectionName));

        services.AddSingleton<IEmbeddingService, AzureOpenAIEmbeddingService>();

        // ---- Azure AI Search (T05) ----
        services.Configure<AzureSearchOptions>(
            configuration.GetSection(AzureSearchOptions.SectionName));

        services.AddSingleton<ISearchService, AzureSearchService>();

        // ---- AI provider selection (T07) ----
        // Each provider is a keyed singleton so it can be retrieved by name.
        // To ADD a new provider: (1) implement IAIProvider, (2) add AddKeyedSingleton below.
        // To SWITCH the active provider: change AI:ProviderName in configuration only — no code change.
        // Azure.AI.OpenAI ChatClient is thread-safe, so Singleton lifetime is appropriate.
        services.Configure<AiProviderOptions>(configuration.GetSection(AiProviderOptions.SectionName));

        services.AddKeyedSingleton<IAIProvider, AzureOpenAiProvider>(AzureOpenAiProvider.ProviderName);
        services.AddKeyedSingleton<IAIProvider, OpenAiProvider>(OpenAiProvider.ProviderName);
        services.AddKeyedSingleton<IAIProvider, AnthropicProvider>(AnthropicProvider.ProviderName);
        services.AddKeyedSingleton<IAIProvider, OllamaProvider>(OllamaProvider.ProviderName);

        // Resolve the active provider at first use, based on the configured name.
        services.AddSingleton<IAIProvider>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<AiProviderOptions>>().Value;
            var providerName = string.IsNullOrWhiteSpace(opts.ProviderName)
                ? AzureOpenAiProvider.ProviderName
                : opts.ProviderName;

            return sp.GetKeyedService<IAIProvider>(providerName)
                   ?? throw new InvalidOperationException(
                       $"Unknown AI provider '{providerName}'. Supported values: {AzureOpenAiProvider.ProviderName}, {OpenAiProvider.ProviderName}, {AnthropicProvider.ProviderName}, {OllamaProvider.ProviderName}.");
        });

        // ---- AI services (T07) ----
        services.AddScoped<IAnalysisService, AnalysisService>();
        services.AddScoped<IComparisonService, ComparisonService>();
        services.AddScoped<IChatService, ChatService>();

        // ---- Export services (T14) ----
        // Each IExportFormatter handles exactly one format; ExportService resolves the right one.
        services.AddTransient<IExportFormatter, MarkdownExportFormatter>();
        services.AddTransient<IExportFormatter, PdfExportFormatter>();
        services.AddTransient<IExportFormatter, WordExportFormatter>();
        services.AddTransient<IExportFormatter, ExcelExportFormatter>();
        services.AddScoped<IExportService, ExportService>();

        // ---- Health checks (T15) ----
        // Registered here (internal types) so they can use internal option classes.
        services.AddHealthChecks()
            .AddCheck<AzureSearchHealthCheck>(
                "azure-search",
                tags: ["ready", "search"])
            .AddCheck<AiProviderHealthCheck>(
                "ai-provider",
                tags: ["ready", "ai"]);

        return services;
    }
}
