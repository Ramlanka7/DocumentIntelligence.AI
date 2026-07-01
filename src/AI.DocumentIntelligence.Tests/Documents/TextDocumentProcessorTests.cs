using AI.DocumentIntelligence.Infrastructure.Documents.Processors;
using FluentAssertions;

namespace AI.DocumentIntelligence.Tests.Documents;

public sealed class TextDocumentProcessorTests
{
    private readonly TextDocumentProcessor _sut = new();

    [Theory]
    [InlineData("document.txt", "text/plain")]
    [InlineData("DOCUMENT.TXT", "application/octet-stream")]
    [InlineData("notes.txt", "text/plain")]
    public void CanProcess_ReturnsTrueForTxtExtensionOrTextPlainContentType(string fileName, string contentType)
    {
        _sut.CanProcess(fileName, contentType).Should().BeTrue();
    }

    [Theory]
    [InlineData("document.pdf", "application/pdf")]
    [InlineData("spreadsheet.csv", "text/csv")]
    public void CanProcess_ReturnsFalseForUnrelatedFormats(string fileName, string contentType)
    {
        _sut.CanProcess(fileName, contentType).Should().BeFalse();
    }

    [Fact]
    public async Task ProcessAsync_WithSimpleText_ReturnsSuccessWithFullText()
    {
        const string content = "Hello world. This is a test document.";
        using var stream = ToStream(content);

        var result = await _sut.ProcessAsync(stream, "test.txt", "text/plain");

        result.IsSuccess.Should().BeTrue();
        result.Value.FullText.Should().Be(content);
    }

    [Fact]
    public async Task ProcessAsync_WithMultipleLines_ProducesPages()
    {
        var lines = Enumerable.Range(1, 60).Select(i => $"Line {i}").ToArray();
        var content = string.Join("\n", lines);
        using var stream = ToStream(content);

        var result = await _sut.ProcessAsync(stream, "test.txt", "text/plain");

        result.IsSuccess.Should().BeTrue();
        result.Value.Pages.Should().HaveCount(2);
        result.Value.Pages[0].PageNumber.Should().Be(1);
        result.Value.Pages[1].PageNumber.Should().Be(2);
    }

    [Fact]
    public async Task ProcessAsync_WithAllCapsHeadings_ExtractsSections()
    {
        const string content = "INTRODUCTION\nThis is the intro.\nCONCLUSION\nThis is the end.";
        using var stream = ToStream(content);

        var result = await _sut.ProcessAsync(stream, "test.txt", "text/plain");

        result.IsSuccess.Should().BeTrue();
        result.Value.Sections.Should().HaveCount(2);
        result.Value.Sections[0].Heading.Should().Be("INTRODUCTION");
        result.Value.Sections[1].Heading.Should().Be("CONCLUSION");
    }

    [Fact]
    public async Task ProcessAsync_WithColonHeadings_ExtractsSections()
    {
        const string content = "Summary:\nSome summary text here.\nDetails:\nDetailed information.";
        using var stream = ToStream(content);

        var result = await _sut.ProcessAsync(stream, "test.txt", "text/plain");

        result.IsSuccess.Should().BeTrue();
        result.Value.Sections.Should().HaveCount(2);
        result.Value.Sections[0].Heading.Should().Be("Summary:");
        result.Value.Sections[1].Heading.Should().Be("Details:");
    }

    [Fact]
    public async Task ProcessAsync_PopulatesMetadata()
    {
        const string content = "Some content";
        using var stream = ToStream(content);

        var result = await _sut.ProcessAsync(stream, "myfile.txt", "text/plain");

        result.IsSuccess.Should().BeTrue();
        result.Value.Metadata.FileName.Should().Be("myfile.txt");
        result.Value.Metadata.ContentType.Should().Be("text/plain");
        result.Value.Metadata.PageCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ProcessAsync_ReturnsNoTables()
    {
        using var stream = ToStream("No tables here.");

        var result = await _sut.ProcessAsync(stream, "test.txt", "text/plain");

        result.IsSuccess.Should().BeTrue();
        result.Value.Tables.Should().BeEmpty();
    }

    private static MemoryStream ToStream(string text)
    {
        var ms = new MemoryStream();
        using var writer = new StreamWriter(ms, leaveOpen: true);
        writer.Write(text);
        writer.Flush();
        ms.Position = 0;
        return ms;
    }
}
