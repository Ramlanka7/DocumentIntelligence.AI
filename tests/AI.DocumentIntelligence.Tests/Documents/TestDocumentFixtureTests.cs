using AI.DocumentIntelligence.Tests.Fixtures;
using FluentAssertions;

namespace AI.DocumentIntelligence.Tests.Documents;

/// <summary>
/// Verifies that each test fixture produces non-empty bytes with the expected file signatures.
/// These smoke tests catch accidental corruption of the fixture data and ensure the static
/// initializers run without error.
/// </summary>
public sealed class TestDocumentFixtureTests
{
    [Fact]
    public void Txt_IsNonEmpty_AndContainsExpectedText()
    {
        TestDocumentFixtures.Txt.Should().NotBeEmpty();
        System.Text.Encoding.UTF8.GetString(TestDocumentFixtures.Txt)
            .Should().Contain("plain-text test fixture");
    }

    [Fact]
    public void Csv_IsNonEmpty_AndContainsCsvHeaders()
    {
        TestDocumentFixtures.Csv.Should().NotBeEmpty();
        System.Text.Encoding.UTF8.GetString(TestDocumentFixtures.Csv)
            .Should().Contain("Name,Age,Department,Salary");
    }

    [Fact]
    public void Pdf_StartsWithPdfMagicBytes()
    {
        TestDocumentFixtures.Pdf.Should().NotBeEmpty();
        // PDF files begin with "%PDF-"
        var header = System.Text.Encoding.ASCII.GetString(TestDocumentFixtures.Pdf[..5]);
        header.Should().Be("%PDF-");
    }

    [Fact]
    public void Docx_StartsWithZipMagicBytes()
    {
        TestDocumentFixtures.Docx.Should().NotBeEmpty();
        // ZIP / DOCX magic number: PK\x03\x04
        TestDocumentFixtures.Docx[0].Should().Be(0x50); // 'P'
        TestDocumentFixtures.Docx[1].Should().Be(0x4B); // 'K'
        TestDocumentFixtures.Docx[2].Should().Be(0x03);
        TestDocumentFixtures.Docx[3].Should().Be(0x04);
    }

    [Fact]
    public void Pptx_StartsWithZipMagicBytes()
    {
        TestDocumentFixtures.Pptx.Should().NotBeEmpty();
        // ZIP / PPTX magic number: PK\x03\x04
        TestDocumentFixtures.Pptx[0].Should().Be(0x50); // 'P'
        TestDocumentFixtures.Pptx[1].Should().Be(0x4B); // 'K'
        TestDocumentFixtures.Pptx[2].Should().Be(0x03);
        TestDocumentFixtures.Pptx[3].Should().Be(0x04);
    }

    [Fact]
    public void TxtStream_CanBeReadToEnd()
    {
        using var stream = TestDocumentFixtures.TxtStream();
        stream.Length.Should().BeGreaterThan(0);
        using var reader = new System.IO.StreamReader(stream);
        reader.ReadToEnd().Should().NotBeEmpty();
    }

    [Fact]
    public void DocxStream_IsReadable_AsZip()
    {
        using var stream = TestDocumentFixtures.DocxStream();
        var act = () =>
        {
            using var zip = new System.IO.Compression.ZipArchive(stream, System.IO.Compression.ZipArchiveMode.Read);
            zip.Entries.Should().NotBeEmpty();
            zip.GetEntry("[Content_Types].xml").Should().NotBeNull();
        };
        act.Should().NotThrow();
    }

    [Fact]
    public void PptxStream_IsReadable_AsZip()
    {
        using var stream = TestDocumentFixtures.PptxStream();
        var act = () =>
        {
            using var zip = new System.IO.Compression.ZipArchive(stream, System.IO.Compression.ZipArchiveMode.Read);
            zip.Entries.Should().NotBeEmpty();
            zip.GetEntry("[Content_Types].xml").Should().NotBeNull();
        };
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(nameof(TestDocumentFixtures.TxtFileName), "test-document.txt")]
    [InlineData(nameof(TestDocumentFixtures.CsvFileName), "test-data.csv")]
    [InlineData(nameof(TestDocumentFixtures.PdfFileName), "test-report.pdf")]
    [InlineData(nameof(TestDocumentFixtures.DocxFileName), "test-contract.docx")]
    [InlineData(nameof(TestDocumentFixtures.PptxFileName), "test-slides.pptx")]
    public void FileNames_HaveExpectedExtensions(string constName, string expectedFileName)
    {
        var prop = typeof(TestDocumentFixtures).GetField(constName,
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        prop.Should().NotBeNull();
        ((string)prop!.GetValue(null)!).Should().Be(expectedFileName);
    }
}
