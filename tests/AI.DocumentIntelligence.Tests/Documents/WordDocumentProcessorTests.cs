using AI.DocumentIntelligence.Infrastructure.Documents.Processors;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using FluentAssertions;

namespace AI.DocumentIntelligence.Tests.Documents;

public sealed class WordDocumentProcessorTests
{
    private readonly WordDocumentProcessor _sut = new();

    [Theory]
    [InlineData("report.docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document")]
    [InlineData("DOCUMENT.DOCX", "text/plain")]
    public void CanProcess_ReturnsTrueForDocxExtensionOrContentType(string fileName, string contentType)
    {
        var byExtension = fileName.EndsWith(".docx", StringComparison.OrdinalIgnoreCase);
        var byMime = contentType == "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
        _sut.CanProcess(fileName, contentType).Should().Be(byExtension || byMime);
    }

    [Fact]
    public void CanProcess_ReturnsFalseForPdf()
    {
        _sut.CanProcess("file.pdf", "application/pdf").Should().BeFalse();
    }

    [Fact]
    public async Task ProcessAsync_WithSimpleDocument_ExtractsParagraphText()
    {
        using var stream = CreateDocxWithParagraphs(["Hello world", "Second paragraph"]);

        var result = await _sut.ProcessAsync(stream, "test.docx",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document");

        result.IsSuccess.Should().BeTrue();
        result.Value.FullText.Should().Contain("Hello world");
        result.Value.FullText.Should().Contain("Second paragraph");
    }

    [Fact]
    public async Task ProcessAsync_WithHeadings_ExtractsSections()
    {
        using var stream = CreateDocxWithHeadings();

        var result = await _sut.ProcessAsync(stream, "test.docx",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document");

        result.IsSuccess.Should().BeTrue();
        result.Value.Sections.Should().HaveCount(2);
        result.Value.Sections[0].Heading.Should().Be("Introduction");
        result.Value.Sections[0].Level.Should().Be(1);
        result.Value.Sections[1].Heading.Should().Be("Conclusion");
        result.Value.Sections[1].Level.Should().Be(2);
    }

    [Fact]
    public async Task ProcessAsync_WithTable_ExtractsTableRows()
    {
        using var stream = CreateDocxWithTable();

        var result = await _sut.ProcessAsync(stream, "test.docx",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document");

        result.IsSuccess.Should().BeTrue();
        result.Value.Tables.Should().HaveCount(1);
        result.Value.Tables[0].Rows.Should().HaveCount(2);
        result.Value.Tables[0].Rows[0].Should().BeEquivalentTo(["Cell A1", "Cell B1"]);
        result.Value.Tables[0].Rows[1].Should().BeEquivalentTo(["Cell A2", "Cell B2"]);
    }

    [Fact]
    public async Task ProcessAsync_PopulatesMetadata()
    {
        using var stream = CreateDocxWithParagraphs(["Content"]);

        var result = await _sut.ProcessAsync(stream, "myreport.docx",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document");

        result.IsSuccess.Should().BeTrue();
        result.Value.Metadata.FileName.Should().Be("myreport.docx");
        result.Value.Metadata.ContentType.Should().Be(
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document");
        result.Value.Metadata.PageCount.Should().BeGreaterThan(0);
    }

    private static MemoryStream CreateDocxWithParagraphs(IEnumerable<string> texts)
    {
        var ms = new MemoryStream();
        using var doc = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document, true);
        var mainPart = doc.AddMainDocumentPart();
        mainPart.Document = new Document(new Body(
            texts.Select(t => new Paragraph(new Run(new Text(t)))).ToArray<OpenXmlElement>()));
        mainPart.Document.Save();
        ms.Position = 0;
        return ms;
    }

    private static MemoryStream CreateDocxWithHeadings()
    {
        var ms = new MemoryStream();
        using var doc = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document, true);
        var mainPart = doc.AddMainDocumentPart();

        var h1Style = new ParagraphProperties(new ParagraphStyleId { Val = "Heading1" });
        var h2Style = new ParagraphProperties(new ParagraphStyleId { Val = "Heading2" });

        mainPart.Document = new Document(new Body(
            new Paragraph(h1Style, new Run(new Text("Introduction"))),
            new Paragraph(new Run(new Text("Intro content."))),
            new Paragraph(h2Style, new Run(new Text("Conclusion"))),
            new Paragraph(new Run(new Text("Conclusion content.")))));
        mainPart.Document.Save();
        ms.Position = 0;
        return ms;
    }

    private static MemoryStream CreateDocxWithTable()
    {
        var ms = new MemoryStream();
        using var doc = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document, true);
        var mainPart = doc.AddMainDocumentPart();

        var table = new Table(
            new TableRow(
                new TableCell(new Paragraph(new Run(new Text("Cell A1")))),
                new TableCell(new Paragraph(new Run(new Text("Cell B1"))))),
            new TableRow(
                new TableCell(new Paragraph(new Run(new Text("Cell A2")))),
                new TableCell(new Paragraph(new Run(new Text("Cell B2"))))));

        mainPart.Document = new Document(new Body(table));
        mainPart.Document.Save();
        ms.Position = 0;
        return ms;
    }
}
