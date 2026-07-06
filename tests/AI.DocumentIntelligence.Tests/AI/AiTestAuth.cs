using AI.DocumentIntelligence.Application.Abstractions.Identity;
using AI.DocumentIntelligence.Application.Abstractions.Persistence;
using AI.DocumentIntelligence.Domain.Entities;
using AI.DocumentIntelligence.Domain.Enums;
using AI.DocumentIntelligence.Domain.ValueObjects;
using AI.DocumentIntelligence.Tests.Integration.Fakes;
using Moq;

namespace AI.DocumentIntelligence.Tests.AI;

/// <summary>
/// Shared arrangement helpers for the AI service tests. The services enforce object-level
/// authorization (document ownership) before any RAG retrieval, so every test that reaches
/// the AI call must authenticate the current user and register the documents it references.
/// </summary>
internal static class AiTestAuth
{
    /// <summary>
    /// Creates a fully processed document owned by <paramref name="ownerId"/> — the state
    /// required for AI operations (unprocessed documents are rejected with Document.NotProcessed).
    /// </summary>
    public static Document NewDocumentOwnedBy(Guid ownerId)
    {
        var document = Document.Create(
            ownerId,
            new FileMetadata("test.pdf", 100, 1, "application/pdf"),
            DocumentType.Pdf,
            "test/path.pdf");

        document.MarkProcessed("test content");
        return document;
    }

    /// <summary>Marks the current-user mock as an authenticated user with the given roles.</summary>
    public static void Authenticate(Mock<ICurrentUser> userMock, Guid userId, params string[] roles)
    {
        userMock.Setup(u => u.IsAuthenticated).Returns(true);
        userMock.Setup(u => u.UserId).Returns(userId);
        userMock.Setup(u => u.Roles).Returns(roles);
    }

    /// <summary>
    /// Wires <c>Repository&lt;Document&gt;()</c> on the unit-of-work mock to an in-memory
    /// repository containing the given documents, satisfying the ownership check.
    /// </summary>
    public static void SetupDocuments(Mock<IUnitOfWork> uowMock, params Document[] documents)
    {
        var repo = new GenericInMemoryRepository<Document>();
        foreach (var document in documents)
        {
            repo.AddAsync(document).GetAwaiter().GetResult();
        }

        uowMock.Setup(u => u.Repository<Document>()).Returns(repo);
    }
}
