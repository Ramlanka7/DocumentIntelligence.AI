using AI.DocumentIntelligence.Application.Abstractions.Documents;
using AI.DocumentIntelligence.Application.Abstractions.Identity;
using AI.DocumentIntelligence.Application.Abstractions.Persistence;
using AI.DocumentIntelligence.Application.Abstractions.Storage;
using AI.DocumentIntelligence.Application.Contracts.Documents;
using AI.DocumentIntelligence.Application.Features.Documents.Upload;
using AI.DocumentIntelligence.Application.Features.RAG.Ingest;
using AI.DocumentIntelligence.Domain.Common;
using AI.DocumentIntelligence.Domain.Enums;
using AI.DocumentIntelligence.Domain.Errors;
using FluentAssertions;
using MediatR;
using Moq;

namespace AI.DocumentIntelligence.Tests.Documents;

/// <summary>Unit tests for <see cref="UploadDocumentCommandHandler"/>.</summary>
public sealed class UploadDocumentCommandHandlerTests
{
    private readonly Mock<IFileUploadValidator> _validator = new();
    private readonly Mock<IDocumentProcessorFactory> _processorFactory = new();
    private readonly Mock<IFileStorage> _fileStorage = new();
    private readonly Mock<IDocumentRepository> _documentRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly Mock<ISender> _sender = new();

    private UploadDocumentCommandHandler CreateHandler() =>
        new(_validator.Object, _processorFactory.Object, _fileStorage.Object,
            _documentRepo.Object, _uow.Object, _currentUser.Object, _sender.Object);

    private static UploadDocumentCommand TextFileCommand(string fileName = "sample.txt") =>
        new(fileName, "text/plain", 100, new MemoryStream("Hello world"u8.ToArray()));

    private void SetupValidUpload(
        string fileName = "sample.txt",
        string extractedText = "Extracted text",
        Guid? userId = null)
    {
        userId ??= Guid.NewGuid();

        _validator.Setup(v => v.Validate(It.IsAny<IReadOnlyList<UploadedFile>>()))
            .Returns(Result.Success());

        var processor = new Mock<IDocumentProcessor>();
        processor.Setup(p => p.ProcessAsync(
                It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new DocumentExtractionResult(
                extractedText,
                [new ExtractedPage(1, extractedText)],
                [],
                [],
                new DocumentMetadata(fileName, "text/plain", 100, 1, null, null))));

        _processorFactory.Setup(f => f.Resolve(fileName, It.IsAny<string>()))
            .Returns(Result.Success(processor.Object));

        _fileStorage.Setup(s => s.SaveAsync(
                It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success($"storage/{fileName}"));

        _currentUser.Setup(u => u.IsAuthenticated).Returns(true);
        _currentUser.Setup(u => u.UserId).Returns(userId);

        _documentRepo.Setup(r => r.AddAsync(It.IsAny<Domain.Entities.Document>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _sender.Setup(s => s.Send(It.IsAny<IngestDocumentCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
    }

    [Fact]
    public async Task Handle_ValidTxtFile_ReturnsSuccess()
    {
        SetupValidUpload();

        var result = await CreateHandler().Handle(TextFileCommand(), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.FileName.Should().Be("sample.txt");
        result.Value.Status.Should().Be(DocumentStatus.Processed);
    }

    [Fact]
    public async Task Handle_ValidationFailure_ReturnsValidationError()
    {
        _validator.Setup(v => v.Validate(It.IsAny<IReadOnlyList<UploadedFile>>()))
            .Returns(Result.Failure(DomainErrors.Upload.UnsupportedFileType));

        var result = await CreateHandler().Handle(TextFileCommand(), default);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be(DomainErrors.Upload.UnsupportedFileType.Code);
    }

    [Fact]
    public async Task Handle_UnsupportedProcessor_ReturnsError()
    {
        _validator.Setup(v => v.Validate(It.IsAny<IReadOnlyList<UploadedFile>>()))
            .Returns(Result.Success());
        _processorFactory.Setup(f => f.Resolve(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Result.Failure<IDocumentProcessor>(DomainErrors.Document.UnsupportedType));

        var result = await CreateHandler().Handle(TextFileCommand(), default);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be(DomainErrors.Document.UnsupportedType.Code);
    }

    [Fact]
    public async Task Handle_UnauthenticatedUser_ReturnsUnauthorized()
    {
        _validator.Setup(v => v.Validate(It.IsAny<IReadOnlyList<UploadedFile>>()))
            .Returns(Result.Success());

        var processor = new Mock<IDocumentProcessor>();
        processor.Setup(p => p.ProcessAsync(
                It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new DocumentExtractionResult(
                "text",
                [new ExtractedPage(1, "text")],
                [],
                [],
                new DocumentMetadata("sample.txt", "text/plain", 100, 1, null, null))));

        _processorFactory.Setup(f => f.Resolve(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Result.Success(processor.Object));

        _fileStorage.Setup(s => s.SaveAsync(
                It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success("storage/sample.txt"));

        _currentUser.Setup(u => u.IsAuthenticated).Returns(false);
        _currentUser.Setup(u => u.UserId).Returns((Guid?)null);

        var result = await CreateHandler().Handle(TextFileCommand(), default);

        result.IsSuccess.Should().BeFalse();
        result.Error.Type.Should().Be(Domain.Common.ErrorType.Unauthorized);
    }

    [Fact]
    public async Task Handle_ValidUpload_IngestsDocument()
    {
        SetupValidUpload();

        await CreateHandler().Handle(TextFileCommand(), default);

        _sender.Verify(
            s => s.Send(It.IsAny<IngestDocumentCommand>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ValidUpload_SavesDocument()
    {
        SetupValidUpload();

        await CreateHandler().Handle(TextFileCommand(), default);

        _documentRepo.Verify(
            r => r.AddAsync(It.IsAny<Domain.Entities.Document>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("report.pdf", DocumentType.Pdf)]
    [InlineData("contract.docx", DocumentType.Docx)]
    [InlineData("notes.txt", DocumentType.Txt)]
    [InlineData("data.csv", DocumentType.Csv)]
    [InlineData("slides.pptx", DocumentType.Pptx)]
    public async Task Handle_DetectsDocumentTypeFromExtension(string fileName, DocumentType expected)
    {
        SetupValidUpload(fileName);

        // Override file validator to accept any type.
        _validator.Setup(v => v.Validate(It.IsAny<IReadOnlyList<UploadedFile>>()))
            .Returns(Result.Success());

        Domain.Entities.Document? saved = null;
        _documentRepo.Setup(r => r.AddAsync(It.IsAny<Domain.Entities.Document>(), It.IsAny<CancellationToken>()))
            .Callback<Domain.Entities.Document, CancellationToken>((doc, _) => saved = doc)
            .Returns(Task.CompletedTask);

        var cmd = new UploadDocumentCommand(fileName, "application/octet-stream", 100,
            new MemoryStream("content"u8.ToArray()));

        await CreateHandler().Handle(cmd, default);

        saved.Should().NotBeNull();
        saved!.Type.Should().Be(expected);
    }
}
