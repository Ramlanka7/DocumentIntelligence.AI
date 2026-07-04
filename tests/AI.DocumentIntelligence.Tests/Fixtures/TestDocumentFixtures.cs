using System.IO.Compression;
using System.Text;

namespace AI.DocumentIntelligence.Tests.Fixtures;

/// <summary>
/// Provides minimal but structurally valid test documents for each supported format.
/// Use these fixtures in unit and integration tests that need real file bytes without
/// depending on external files checked into the repository.
/// </summary>
public static class TestDocumentFixtures
{
    // ---- Friendly file names ----

    public const string TxtFileName = "test-document.txt";
    public const string CsvFileName = "test-data.csv";
    public const string PdfFileName = "test-report.pdf";
    public const string DocxFileName = "test-contract.docx";
    public const string PptxFileName = "test-slides.pptx";

    // ---- MIME content types ----

    public const string TxtContentType = "text/plain";
    public const string CsvContentType = "text/csv";
    public const string PdfContentType = "application/pdf";

    public const string DocxContentType =
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

    public const string PptxContentType =
        "application/vnd.openxmlformats-officedocument.presentationml.presentation";

    // ---- Raw byte content ----

    /// <summary>Plain UTF-8 text fixture (easily extracted by <c>TxtDocumentProcessor</c>).</summary>
    public static byte[] Txt { get; } = Encoding.UTF8.GetBytes(
        "Test document title\n\n" +
        "This is a plain-text test fixture used by the Document Intelligence test suite.\n" +
        "It contains multiple lines so that the chunking service has realistic input.\n\n" +
        "Section 1: Introduction\n" +
        "The quick brown fox jumps over the lazy dog.\n\n" +
        "Section 2: Conclusion\n" +
        "Testing ensures quality.\n");

    /// <summary>CSV fixture with headers and three data rows.</summary>
    public static byte[] Csv { get; } = Encoding.UTF8.GetBytes(
        "Name,Age,Department,Salary\n" +
        "Alice,30,Engineering,95000\n" +
        "Bob,25,Marketing,72000\n" +
        "Charlie,35,Finance,88000\n");

    /// <summary>Minimal structurally-valid PDF-1.4 document with one empty page.</summary>
    public static byte[] Pdf { get; } = BuildMinimalPdf();

    /// <summary>Minimal valid DOCX (Office Open XML) archive with one paragraph of text.</summary>
    public static byte[] Docx { get; } = BuildMinimalDocx();

    /// <summary>Minimal valid PPTX (Office Open XML) archive with one text slide.</summary>
    public static byte[] Pptx { get; } = BuildMinimalPptx();

    // ---- Stream factory helpers ----

    /// <summary>Returns a new <see cref="MemoryStream"/> over the TXT fixture bytes.</summary>
    public static MemoryStream TxtStream() => new(Txt, writable: false);

    /// <summary>Returns a new <see cref="MemoryStream"/> over the CSV fixture bytes.</summary>
    public static MemoryStream CsvStream() => new(Csv, writable: false);

    /// <summary>Returns a new <see cref="MemoryStream"/> over the PDF fixture bytes.</summary>
    public static MemoryStream PdfStream() => new(Pdf, writable: false);

    /// <summary>Returns a new <see cref="MemoryStream"/> over the DOCX fixture bytes.</summary>
    public static MemoryStream DocxStream() => new(Docx, writable: false);

    /// <summary>Returns a new <see cref="MemoryStream"/> over the PPTX fixture bytes.</summary>
    public static MemoryStream PptxStream() => new(Pptx, writable: false);

    // ---- Private builders ----

    private static byte[] BuildMinimalPdf()
    {
        // Produces a minimal but standards-compliant PDF-1.4 with one blank page.
        const string pdf =
            "%PDF-1.4\n" +
            "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n" +
            "2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n" +
            "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] >>\nendobj\n" +
            "xref\n" +
            "0 4\n" +
            "0000000000 65535 f \n" +
            "0000000009 00000 n \n" +
            "0000000068 00000 n \n" +
            "0000000125 00000 n \n" +
            "trailer\n<< /Size 4 /Root 1 0 R >>\n" +
            "startxref\n" +
            "220\n" +
            "%%EOF\n";
        return Encoding.ASCII.GetBytes(pdf);
    }

    private static byte[] BuildMinimalDocx()
    {
        // DOCX is a ZIP archive. We produce the minimum set of parts required by the spec.
        using var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            AddZipEntry(zip, "[Content_Types].xml",
                "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\n" +
                "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">\n" +
                "  <Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>\n" +
                "  <Default Extension=\"xml\" ContentType=\"application/xml\"/>\n" +
                "  <Override PartName=\"/word/document.xml\"\n" +
                "    ContentType=\"application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml\"/>\n" +
                "</Types>");

            AddZipEntry(zip, "_rels/.rels",
                "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\n" +
                "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">\n" +
                "  <Relationship Id=\"rId1\"\n" +
                "    Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\"\n" +
                "    Target=\"word/document.xml\"/>\n" +
                "</Relationships>");

            AddZipEntry(zip, "word/document.xml",
                "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\n" +
                "<w:document xmlns:wpc=\"http://schemas.microsoft.com/office/word/2010/wordprocessingCanvas\"\n" +
                "  xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\">\n" +
                "  <w:body>\n" +
                "    <w:p>\n" +
                "      <w:r><w:t>Test DOCX fixture content for Document Intelligence platform.</w:t></w:r>\n" +
                "    </w:p>\n" +
                "    <w:sectPr/>\n" +
                "  </w:body>\n" +
                "</w:document>");

            AddZipEntry(zip, "word/_rels/document.xml.rels",
                "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\n" +
                "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">\n" +
                "</Relationships>");
        }

        return ms.ToArray();
    }

    private static byte[] BuildMinimalPptx()
    {
        // PPTX is also a ZIP archive with a presentation and one slide.
        using var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            AddZipEntry(zip, "[Content_Types].xml",
                "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\n" +
                "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">\n" +
                "  <Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>\n" +
                "  <Default Extension=\"xml\" ContentType=\"application/xml\"/>\n" +
                "  <Override PartName=\"/ppt/presentation.xml\"\n" +
                "    ContentType=\"application/vnd.openxmlformats-officedocument.presentationml.presentation.main+xml\"/>\n" +
                "  <Override PartName=\"/ppt/slides/slide1.xml\"\n" +
                "    ContentType=\"application/vnd.openxmlformats-officedocument.presentationml.slide+xml\"/>\n" +
                "</Types>");

            AddZipEntry(zip, "_rels/.rels",
                "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\n" +
                "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">\n" +
                "  <Relationship Id=\"rId1\"\n" +
                "    Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\"\n" +
                "    Target=\"ppt/presentation.xml\"/>\n" +
                "</Relationships>");

            AddZipEntry(zip, "ppt/presentation.xml",
                "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\n" +
                "<p:presentation xmlns:p=\"http://schemas.openxmlformats.org/presentationml/2006/main\"\n" +
                "  xmlns:a=\"http://schemas.openxmlformats.org/drawingml/2006/main\">\n" +
                "  <p:sldMasterIdLst/>\n" +
                "  <p:sldSz cx=\"9144000\" cy=\"6858000\"/>\n" +
                "  <p:notesSz cx=\"6858000\" cy=\"9144000\"/>\n" +
                "  <p:sldIdLst>\n" +
                "    <p:sldId id=\"256\" r:id=\"rId1\"\n" +
                "      xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"/>\n" +
                "  </p:sldIdLst>\n" +
                "</p:presentation>");

            AddZipEntry(zip, "ppt/_rels/presentation.xml.rels",
                "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\n" +
                "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">\n" +
                "  <Relationship Id=\"rId1\"\n" +
                "    Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/slide\"\n" +
                "    Target=\"slides/slide1.xml\"/>\n" +
                "</Relationships>");

            AddZipEntry(zip, "ppt/slides/slide1.xml",
                "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\n" +
                "<p:sld xmlns:p=\"http://schemas.openxmlformats.org/presentationml/2006/main\"\n" +
                "  xmlns:a=\"http://schemas.openxmlformats.org/drawingml/2006/main\">\n" +
                "  <p:cSld>\n" +
                "    <p:spTree>\n" +
                "      <p:sp>\n" +
                "        <p:txBody>\n" +
                "          <a:bodyPr/>\n" +
                "          <a:p><a:r><a:t>Test PPTX slide content for Document Intelligence platform.</a:t></a:r></a:p>\n" +
                "        </p:txBody>\n" +
                "      </p:sp>\n" +
                "    </p:spTree>\n" +
                "  </p:cSld>\n" +
                "</p:sld>");

            AddZipEntry(zip, "ppt/slides/_rels/slide1.xml.rels",
                "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\n" +
                "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">\n" +
                "</Relationships>");
        }

        return ms.ToArray();
    }

    private static void AddZipEntry(ZipArchive zip, string path, string content)
    {
        var entry = zip.CreateEntry(path, CompressionLevel.Fastest);
        using var writer = new StreamWriter(entry.Open(), Encoding.UTF8);
        writer.Write(content);
    }
}
