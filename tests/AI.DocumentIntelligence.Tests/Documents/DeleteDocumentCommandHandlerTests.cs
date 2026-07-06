using AI.DocumentIntelligence.Application.Abstractions.Identity;
using AI.DocumentIntelligence.Application.Abstractions.Persistence;
using AI.DocumentIntelligence.Application.Abstractions.Search;
using AI.DocumentIntelligence.Application.Abstractions.Storage;
using AI.DocumentIntelligence.Application.Features.Documents.Delete;
using AI.DocumentIntelligence.Domain.Common;
using AI.DocumentIntelligence.Domain.Entities;
using AI.DocumentIntelligence.Domain.Enums;
using AI.DocumentIntelligence.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace AI.DocumentIntelligence.Tests.Documents;

/// <summary>
/// Tests for <see cref="DeleteDocumentCommandHandler"/>: owner-or-admin authorization, the
/// DB-first delete ordering, and best-effort cleanup semantics (a storage or index failure
/// must never resurrect an already-deleted document).
/// </summary>
public sealed class DeleteDocumentCommandHandlerTests
{
    private readonly Mock<IDocumentRepository> _documentRepo = new();
    private readonly Mock<IFileStorage> _fileStorage = new();
    private readonly Mock<ISearchService> _searchService = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly Guid _userId = Guid.NewGuid();

    private DeleteDocumentCommandHandler CreateHandler() =>
        new(_documentRepo.Object, _fileStorage.Object, _searchService.Object,
            _uow.Object, _currentUser.Object,
            NullLogger<DeleteDocumentCommandHandler>.Instance);

    private static Document CreateDocument(Guid ownerId) =>
        Document.Create(
            ownerId,
            new FileMetadata("report.pdf", 100, 2, "application/pdf"),
            DocumentType.Pdf,
            "storage/report.pdf");

    private void Authenticate(Guid userId, params string[] roles)
    {
        _currentUser.Setup(u => u.IsAuthenticated).Returns(true);
        _currentUser.Setup(u => u.UserId).Returns(userId);
        _currentUser.Setup(u => u.Roles).Returns(roles);
    }

    private void SetupHappyCleanup()
    {
        _fileStorage.Setup(s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
        _searchService.Setup(s => s.DeleteByDocumentAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    [Fact]
    public async Task Handle_MissingDocument_ReturnsNotFound()
    {
        Authenticate(_userId, "Analyst");
        _documentRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Document?)null);

        var result = await CreateHandler().Handle(new DeleteDocumentCommand(Guid.NewGuid()), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Document.NotFound");
    }

    [Fact]
    public async Task Handle_NonOwner_ReturnsForbiddenAndDeletesNothing()
    {
        Authenticate(_userId, "Analyst");
        var foreignDocument = CreateDocument(Guid.NewGuid());
        _documentRepo.Setup(r => r.GetByIdAsync(foreignDocument.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(foreignDocument);

        var result = await CreateHandler().Handle(new DeleteDocumentCommand(foreignDocument.Id), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Document.Forbidden");
        _documentRepo.Verify(r => r.Remove(It.IsAny<Document>()), Times.Never);
        _fileStorage.Verify(
            s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Owner_RemovesEntityThenCleansUpStorageAndIndex()
    {
        Authenticate(_userId, "Analyst");
        var document = CreateDocument(_userId);
        _documentRepo.Setup(r => r.GetByIdAsync(document.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);
        SetupHappyCleanup();

        var result = await CreateHandler().Handle(new DeleteDocumentCommand(document.Id), default);

        result.IsSuccess.Should().BeTrue();
        _documentRepo.Verify(r => r.Remove(document), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _searchService.Verify(
            s => s.DeleteByDocumentAsync(document.Id, It.IsAny<CancellationToken>()), Times.Once);
        _fileStorage.Verify(
            s => s.DeleteAsync("storage/report.pdf", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_AdminDeletesForeignDocument_Succeeds()
    {
        Authenticate(_userId, "Admin");
        var foreignDocument = CreateDocument(Guid.NewGuid());
        _documentRepo.Setup(r => r.GetByIdAsync(foreignDocument.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(foreignDocument);
        SetupHappyCleanup();

        var result = await CreateHandler().Handle(new DeleteDocumentCommand(foreignDocument.Id), default);

        result.IsSuccess.Should().BeTrue();
        _documentRepo.Verify(r => r.Remove(foreignDocument), Times.Once);
    }

    [Fact]
    public async Task Handle_CleanupFailures_StillReturnSuccess()
    {
        // The DB row is the source of truth; orphaned blobs/index entries are logged,
        // not surfaced as errors after the document is already gone.
        Authenticate(_userId, "Analyst");
        var document = CreateDocument(_userId);
        _documentRepo.Setup(r => r.GetByIdAsync(document.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _searchService.Setup(s => s.DeleteByDocumentAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(Error.Failure("Search.Down", "index unavailable")));
        _fileStorage.Setup(s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(Error.Failure("Storage.Down", "blob unavailable")));

        var result = await CreateHandler().Handle(new DeleteDocumentCommand(document.Id), default);

        result.IsSuccess.Should().BeTrue();
        _documentRepo.Verify(r => r.Remove(document), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
