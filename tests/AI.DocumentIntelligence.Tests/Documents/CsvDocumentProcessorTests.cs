using AI.DocumentIntelligence.Infrastructure.Documents.Processors;
using FluentAssertions;

namespace AI.DocumentIntelligence.Tests.Documents;

public sealed class CsvDocumentProcessorTests
{
    private readonly CsvDocumentProcessor _sut = new();

    [Theory]
    [InlineData("data.csv", "text/csv")]
    [InlineData("DATA.CSV", "application/csv")]
    [InlineData("export.csv", "text/plain")]
    public void CanProcess_ReturnsTrueForCsvExtension(string fileName, string contentType)
    {
        _sut.CanProcess(fileName, "text/csv").Should().BeTrue();
        _sut.CanProcess(fileName, contentType).Should().Be(
            fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase)
            || contentType is "text/csv" or "application/csv");
    }

    [Theory]
    [InlineData("document.pdf", "application/pdf")]
    [InlineData("document.txt", "text/plain")]
    public void CanProcess_ReturnsFalseForNonCsvFormats(string fileName, string contentType)
    {
        _sut.CanProcess(fileName, contentType).Should().BeFalse();
    }

    [Fact]
    public async Task ProcessAsync_WithHeaderAndDataRows_ExtractsTable()
    {
        const string csv = "Name,Age,City\nAlice,30,London\nBob,25,Paris";
        using var stream = ToStream(csv);

        var result = await _sut.ProcessAsync(stream, "people.csv", "text/csv");

        result.IsSuccess.Should().BeTrue();
        result.Value.Tables.Should().HaveCount(1);
        var table = result.Value.Tables[0];
        table.PageNumber.Should().Be(1);
        table.Rows.Should().HaveCount(3);
        table.Rows[0].Should().BeEquivalentTo(["Name", "Age", "City"]);
        table.Rows[1].Should().BeEquivalentTo(["Alice", "30", "London"]);
        table.Rows[2].Should().BeEquivalentTo(["Bob", "25", "Paris"]);
    }

    [Fact]
    public async Task ProcessAsync_WithCsvData_FullTextContainsAllRows()
    {
        const string csv = "Col1,Col2\nVal1,Val2";
        using var stream = ToStream(csv);

        var result = await _sut.ProcessAsync(stream, "data.csv", "text/csv");

        result.IsSuccess.Should().BeTrue();
        result.Value.FullText.Should().Contain("Col1");
        result.Value.FullText.Should().Contain("Val1");
    }

    [Fact]
    public async Task ProcessAsync_AlwaysReturnsSinglePage()
    {
        const string csv = "A,B\n1,2\n3,4";
        using var stream = ToStream(csv);

        var result = await _sut.ProcessAsync(stream, "data.csv", "text/csv");

        result.IsSuccess.Should().BeTrue();
        result.Value.Pages.Should().HaveCount(1);
        result.Value.Pages[0].PageNumber.Should().Be(1);
        result.Value.Metadata.PageCount.Should().Be(1);
    }

    [Fact]
    public async Task ProcessAsync_ReturnsNoSections()
    {
        const string csv = "X,Y\n1,2";
        using var stream = ToStream(csv);

        var result = await _sut.ProcessAsync(stream, "data.csv", "text/csv");

        result.IsSuccess.Should().BeTrue();
        result.Value.Sections.Should().BeEmpty();
    }

    [Fact]
    public async Task ProcessAsync_PopulatesMetadata()
    {
        const string csv = "A,B\n1,2";
        using var stream = ToStream(csv);

        var result = await _sut.ProcessAsync(stream, "report.csv", "text/csv");

        result.IsSuccess.Should().BeTrue();
        result.Value.Metadata.FileName.Should().Be("report.csv");
        result.Value.Metadata.ContentType.Should().Be("text/csv");
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
