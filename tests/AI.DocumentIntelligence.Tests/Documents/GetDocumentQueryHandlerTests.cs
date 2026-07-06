using AI.DocumentIntelligence.Application.Abstractions.Identity;
using AI.DocumentIntelligence.Application.Abstractions.Persistence;
using AI.DocumentIntelligence.Application.Features.Documents.Queries;
using AI.DocumentIntelligence.Domain.Entities;
using AI.DocumentIntelligence.Domain.Enums;
using AI.DocumentIntelligence.Domain.ValueObjects;
using FluentAssertions;
using Moq;

namespace AI.DocumentIntelligence.Tests.Documents;

/// <summary>
/// Object-level authorization tests for <see cref="GetDocumentQueryHandler"/>: only the
/// owner (or an admin) may read a document's detail; everyone else gets NotFound so that
/// document IDs are not enumerable.
/// </summary>
public sealed class GetDocumentQueryHandlerTests
{
    private readonly Mock<IDocumentRepository> _documentRepo = new();
    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly Guid _userId = Guid.NewGuid();

    private GetDocumentQueryHandler CreateHandler() =>
        new(_documentRepo.Object, _currentUser.Object);

    private static Document CreateDocument(Guid ownerId)
    {
        var document = Document.Create(
            ownerId,
            new FileMetadata("contract.pdf", 1234, 10, "application/pdf"),
            DocumentType.Pdf,
            "storage/contract.pdf");
        document.MarkProcessed("text");
        return document;
    }

    private void Authenticate(Guid userId, params string[] roles)
    {
        _currentUser.Setup(u => u.IsAuthenticated).Returns(true);
        _currentUser.Setup(u => u.UserId).Returns(userId);
        _currentUser.Setup(u => u.Roles).Returns(roles);
    }

    [Fact]
    public async Task Handle_Unauthenticated_ReturnsUnauthorized()
    {
        _currentUser.Setup(u => u.IsAuthenticated).Returns(false);
        _currentUser.Setup(u => u.UserId).Returns((Guid?)null);

        var result = await CreateHandler().Handle(new GetDocumentQuery(Guid.NewGuid()), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Auth.NotAuthenticated");
    }

    [Fact]
    public async Task Handle_OwnerRequestsOwnDocument_ReturnsDetail()
    {
        Authenticate(_userId, "Viewer");
        var document = CreateDocument(_userId);
        _documentRepo.Setup(r => r.GetByIdAsync(document.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        var result = await CreateHandler().Handle(new GetDocumentQuery(document.Id), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(document.Id);
        result.Value.FileName.Should().Be("contract.pdf");
        result.Value.Status.Should().Be(DocumentStatus.Processed);
    }

    [Fact]
    public async Task Handle_NonOwnerRequestsForeignDocument_ReturnsNotFound()
    {
        Authenticate(_userId, "Analyst");
        var foreignDocument = CreateDocument(Guid.NewGuid());
        _documentRepo.Setup(r => r.GetByIdAsync(foreignDocument.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(foreignDocument);

        var result = await CreateHandler().Handle(new GetDocumentQuery(foreignDocument.Id), default);

        // NotFound (not Forbidden) so foreign document IDs cannot be confirmed to exist.
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Document.NotFound");
    }

    [Fact]
    public async Task Handle_AdminRequestsForeignDocument_ReturnsDetail()
    {
        Authenticate(_userId, "Admin");
        var foreignDocument = CreateDocument(Guid.NewGuid());
        _documentRepo.Setup(r => r.GetByIdAsync(foreignDocument.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(foreignDocument);

        var result = await CreateHandler().Handle(new GetDocumentQuery(foreignDocument.Id), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(foreignDocument.Id);
    }

    [Fact]
    public async Task Handle_MissingDocument_ReturnsNotFound()
    {
        Authenticate(_userId, "Viewer");
        _documentRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Document?)null);

        var result = await CreateHandler().Handle(new GetDocumentQuery(Guid.NewGuid()), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Document.NotFound");
    }
}
