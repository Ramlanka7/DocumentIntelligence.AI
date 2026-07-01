using AI.DocumentIntelligence.Application.Abstractions.Documents;
using AI.DocumentIntelligence.Domain.Errors;
using AI.DocumentIntelligence.Infrastructure.Auth;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace AI.DocumentIntelligence.Tests.Auth;

public sealed class FileUploadValidatorTests
{
    private static readonly UploadOptions DefaultOptions = new()
    {
        MaxDocuments = 4,
        MaxFileSizeBytes = 10_000,
        MaxCombinedSizeBytes = 30_000,
        MaxCombinedPages = 500,
    };

    private static FileUploadValidator CreateValidator(UploadOptions? opts = null) =>
        new(Options.Create(opts ?? DefaultOptions));

    // ---- Factory helpers for magic-byte streams ----

    private static MemoryStream PdfStream() =>
        new([0x25, 0x50, 0x44, 0x46, 0x00]); // %PDF

    private static MemoryStream ZipStream() =>
        new([0x50, 0x4B, 0x03, 0x04, 0x00]); // PK (DOCX/PPTX)

    private static MemoryStream TextStream() =>
        new("hello world\n"u8.ToArray());

    private static MemoryStream BinaryJunkStream() =>
        new([0x00, 0x01, 0x02, 0x03, 0x04]);

    private static UploadedFile MakePdf(long size = 100, int pages = 1) =>
        new("doc.pdf", "application/pdf", size, pages, PdfStream());

    private static UploadedFile MakeDocx(long size = 100, int pages = 1) =>
        new("doc.docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document", size, pages, ZipStream());

    private static UploadedFile MakeTxt(long size = 50, int pages = 1) =>
        new("note.txt", "text/plain", size, pages, TextStream());

    private static UploadedFile MakeBinary() =>
        new("evil.exe", "application/octet-stream", 100, 1, BinaryJunkStream());

    // ---- Tests ----

    [Fact]
    public void Validate_SinglePdf_Succeeds()
    {
        var result = CreateValidator().Validate([MakePdf()]);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Validate_SingleDocx_Succeeds()
    {
        var result = CreateValidator().Validate([MakeDocx()]);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Validate_SingleTextFile_Succeeds()
    {
        var result = CreateValidator().Validate([MakeTxt()]);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Validate_TooManyDocuments_ReturnsFailure()
    {
        var files = Enumerable.Range(0, 5).Select(_ => MakePdf()).ToList();

        var result = CreateValidator().Validate(files);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be(DomainErrors.Upload.TooManyDocuments.Code);
    }

    [Fact]
    public void Validate_ExactlyMaxDocuments_Succeeds()
    {
        var files = Enumerable.Range(0, 4).Select(_ => MakePdf()).ToList();

        var result = CreateValidator().Validate(files);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Validate_FileSizeExceeded_ReturnsFailure()
    {
        var file = MakePdf(size: DefaultOptions.MaxFileSizeBytes + 1);

        var result = CreateValidator().Validate([file]);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be(DomainErrors.Upload.FileSizeExceeded.Code);
    }

    [Fact]
    public void Validate_CombinedSizeExceeded_ReturnsFailure()
    {
        // Each file is within per-file limit but combined they exceed the cap.
        var opts = new UploadOptions
        {
            MaxDocuments = 4,
            MaxFileSizeBytes = 10_000,
            MaxCombinedSizeBytes = 150,
            MaxCombinedPages = 500,
        };
        var files = new List<UploadedFile>
        {
            MakePdf(size: 100),
            MakePdf(size: 100),
        };

        var result = CreateValidator(opts).Validate(files);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be(DomainErrors.Upload.CombinedSizeExceeded.Code);
    }

    [Fact]
    public void Validate_CombinedPagesExceeded_ReturnsFailure()
    {
        var opts = new UploadOptions
        {
            MaxDocuments = 4,
            MaxFileSizeBytes = 10_000,
            MaxCombinedSizeBytes = 30_000,
            MaxCombinedPages = 10,
        };
        var files = new List<UploadedFile>
        {
            MakePdf(pages: 6),
            MakePdf(pages: 6),
        };

        var result = CreateValidator(opts).Validate(files);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be(DomainErrors.Upload.CombinedPageLimitExceeded.Code);
    }

    [Fact]
    public void Validate_UnsupportedFileType_ReturnsFailure()
    {
        var result = CreateValidator().Validate([MakeBinary()]);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be(DomainErrors.Upload.UnsupportedFileType.Code);
    }

    [Fact]
    public void Validate_EmptyBatch_Succeeds()
    {
        var result = CreateValidator().Validate([]);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Validate_MixedValidTypes_Succeeds()
    {
        var files = new List<UploadedFile> { MakePdf(), MakeDocx(), MakeTxt() };

        var result = CreateValidator().Validate(files);

        result.IsSuccess.Should().BeTrue();
    }
}
