using AI.DocumentIntelligence.Application.Abstractions.Documents;
using AI.DocumentIntelligence.Infrastructure.Documents;
using AI.DocumentIntelligence.Infrastructure.Documents.Processors;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace AI.DocumentIntelligence.Tests.Documents;

public sealed class DocumentProcessorFactoryTests
{
    private static IDocumentProcessorFactory BuildFactory()
    {
        var services = new ServiceCollection();
        services.AddTransient<IDocumentProcessor, PdfDocumentProcessor>();
        services.AddTransient<IDocumentProcessor, WordDocumentProcessor>();
        services.AddTransient<IDocumentProcessor, TextDocumentProcessor>();
        services.AddTransient<IDocumentProcessor, CsvDocumentProcessor>();
        services.AddTransient<IDocumentProcessor, PowerPointDocumentProcessor>();
        services.AddTransient<IDocumentProcessorFactory, DocumentProcessorFactory>();
        return services.BuildServiceProvider().GetRequiredService<IDocumentProcessorFactory>();
    }

    [Theory]
    [InlineData("document.pdf", "application/pdf", typeof(PdfDocumentProcessor))]
    [InlineData("report.docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document", typeof(WordDocumentProcessor))]
    [InlineData("notes.txt", "text/plain", typeof(TextDocumentProcessor))]
    [InlineData("data.csv", "text/csv", typeof(CsvDocumentProcessor))]
    [InlineData("slides.pptx", "application/vnd.openxmlformats-officedocument.presentationml.presentation", typeof(PowerPointDocumentProcessor))]
    public void Resolve_ReturnsCorrectProcessorForEachFormat(string fileName, string contentType, Type expectedType)
    {
        var factory = BuildFactory();

        var result = factory.Resolve(fileName, contentType);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeOfType(expectedType);
    }

    [Fact]
    public void Resolve_ReturnsFailure_ForUnknownFormat()
    {
        var factory = BuildFactory();

        var result = factory.Resolve("unknown.xyz", "application/unknown");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Document.Processor.NotFound");
    }

    [Fact]
    public void Resolve_MatchesByExtension_RegardlessOfContentType()
    {
        var factory = BuildFactory();

        var result = factory.Resolve("file.pdf", "application/octet-stream");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeOfType<PdfDocumentProcessor>();
    }

    [Fact]
    public void Resolve_MatchesByContentType_RegardlessOfExtension()
    {
        var factory = BuildFactory();

        var result = factory.Resolve("noextension", "text/csv");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeOfType<CsvDocumentProcessor>();
    }
}
