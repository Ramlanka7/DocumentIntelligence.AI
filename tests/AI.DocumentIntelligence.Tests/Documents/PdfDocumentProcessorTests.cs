using AI.DocumentIntelligence.Infrastructure.Documents.Processors;
using FluentAssertions;

namespace AI.DocumentIntelligence.Tests.Documents;

public sealed class PdfDocumentProcessorTests
{
    private readonly PdfDocumentProcessor _sut = new();

    // A minimal valid single-page PDF with the text "Hello PDF" on page 1.
    // This byte array was constructed as a conformant PDF 1.4 object.
    private static readonly byte[] MinimalPdf = CreateMinimalPdf("Hello PDF");

    [Theory]
    [InlineData("report.pdf", "application/pdf")]
    [InlineData("REPORT.PDF", "text/plain")]
    [InlineData("document.txt", "application/pdf")]
    public void CanProcess_ReturnsTrueForPdfExtensionOrContentType(string fileName, string contentType)
    {
        var byExtension = fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase);
        var byMime = contentType == "application/pdf";
        _sut.CanProcess(fileName, contentType).Should().Be(byExtension || byMime);
    }

    [Theory]
    [InlineData("report.docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document")]
    [InlineData("data.csv", "text/csv")]
    public void CanProcess_ReturnsFalseForNonPdfFormats(string fileName, string contentType)
    {
        _sut.CanProcess(fileName, contentType).Should().BeFalse();
    }

    [Fact]
    public async Task ProcessAsync_WithMinimalPdf_ReturnsSuccess()
    {
        using var stream = new MemoryStream(MinimalPdf);

        var result = await _sut.ProcessAsync(stream, "test.pdf", "application/pdf");

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ProcessAsync_WithMinimalPdf_ReturnsOnePage()
    {
        using var stream = new MemoryStream(MinimalPdf);

        var result = await _sut.ProcessAsync(stream, "test.pdf", "application/pdf");

        result.IsSuccess.Should().BeTrue();
        result.Value.Pages.Should().HaveCount(1);
        result.Value.Pages[0].PageNumber.Should().Be(1);
        result.Value.Metadata.PageCount.Should().Be(1);
    }

    [Fact]
    public async Task ProcessAsync_WithMinimalPdf_PopulatesMetadata()
    {
        using var stream = new MemoryStream(MinimalPdf);

        var result = await _sut.ProcessAsync(stream, "mypdf.pdf", "application/pdf");

        result.IsSuccess.Should().BeTrue();
        result.Value.Metadata.FileName.Should().Be("mypdf.pdf");
        result.Value.Metadata.ContentType.Should().Be("application/pdf");
    }

    [Fact]
    public async Task ProcessAsync_WithCorruptData_ReturnsFailure()
    {
        var corrupt = "This is not a valid PDF file content"u8.ToArray();
        using var stream = new MemoryStream(corrupt);

        var result = await _sut.ProcessAsync(stream, "corrupt.pdf", "application/pdf");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().StartWith("Document.Pdf.");
    }

    private static byte[] CreateMinimalPdf(string pageText)
    {
        // A hand-crafted minimal PDF 1.4 document with one page containing the given text.
        var content = $"BT /F1 12 Tf 100 700 Td ({pageText}) Tj ET";
        var contentBytes = System.Text.Encoding.Latin1.GetBytes(content);
        var contentLength = contentBytes.Length;

        using var ms = new MemoryStream();
        using var w = new StreamWriter(ms, System.Text.Encoding.Latin1, leaveOpen: true);

        var offsets = new int[5];

        w.Write("%PDF-1.4\n");
        w.Flush();

        offsets[1] = (int)ms.Length;
        w.Write("1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");
        w.Flush();

        offsets[2] = (int)ms.Length;
        w.Write("2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n");
        w.Flush();

        offsets[3] = (int)ms.Length;
        w.Write(
            "3 0 obj\n" +
            "<< /Type /Page /Parent 2 0 R " +
            "/MediaBox [0 0 612 792] " +
            "/Contents 4 0 R " +
            "/Resources << /Font << /F1 << /Type /Font /Subtype /Type1 /BaseFont /Helvetica >> >> >> " +
            ">>\nendobj\n");
        w.Flush();

        offsets[4] = (int)ms.Length;
        w.Write($"4 0 obj\n<< /Length {contentLength} >>\nstream\n");
        w.Flush();
        ms.Write(contentBytes);
        w.Write("\nendstream\nendobj\n");
        w.Flush();

        var xrefOffset = (int)ms.Length;
        w.Write("xref\n");
        w.Write("0 5\n");
        w.Write("0000000000 65535 f \n");
        for (var i = 1; i <= 4; i++)
        {
            w.Write($"{offsets[i]:D10} 00000 n \n");
        }

        w.Write("trailer\n<< /Size 5 /Root 1 0 R >>\n");
        w.Write($"startxref\n{xrefOffset}\n%%EOF\n");
        w.Flush();

        return ms.ToArray();
    }
}
