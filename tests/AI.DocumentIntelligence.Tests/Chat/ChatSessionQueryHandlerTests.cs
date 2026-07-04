using AI.DocumentIntelligence.Application.Abstractions.Identity;
using AI.DocumentIntelligence.Application.Abstractions.Persistence;
using AI.DocumentIntelligence.Application.Features.Chat.DeleteChatSession;
using AI.DocumentIntelligence.Application.Features.Chat.GetChatSession;
using AI.DocumentIntelligence.Application.Features.Chat.GetChatSessions;
using AI.DocumentIntelligence.Domain.Entities;
using AI.DocumentIntelligence.Domain.Enums;
using AI.DocumentIntelligence.Tests.Integration.Fakes;
using FluentAssertions;
using Moq;

namespace AI.DocumentIntelligence.Tests.Chat;

/// <summary>
/// Unit tests for chat session read/delete query handlers — owner-scoping and 404 behaviour.
/// </summary>
public sealed class ChatSessionQueryHandlerTests
{
    private readonly InMemoryUnitOfWork _uow = new();
    private readonly InMemoryChatSessionRepository _chatRepo = new();
    private readonly Mock<ICurrentUser> _userMock = new();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _otherUserId = Guid.NewGuid();

    private IUnitOfWork Uow => _uow;

    public ChatSessionQueryHandlerTests()
    {
        _userMock.Setup(u => u.UserId).Returns(_userId);
    }

    /// <summary>Adds a session to both the generic unit of work (for delete) and the
    /// dedicated chat-session repository (for owner-scoped, message-loaded reads).</summary>
    private async Task AddSessionAsync(ChatSession session)
    {
        await Uow.Repository<ChatSession>().AddAsync(session);
        _chatRepo.Add(session);
    }

    // ── GetChatSessions ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetChatSessions_ReturnsOnlyCurrentUserSessions()
    {
        // Arrange — two sessions for current user, one for another user
        var ownedSession1 = CreateSession(_userId);
        var ownedSession2 = CreateSession(_userId);
        var foreignSession = CreateSession(_otherUserId);

        await AddSessionAsync(ownedSession1);
        await AddSessionAsync(ownedSession2);
        await AddSessionAsync(foreignSession);

        var handler = new GetChatSessionsQueryHandler(_chatRepo, _userMock.Object);
        var result = await handler.Handle(new GetChatSessionsQuery(), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Should().AllSatisfy(s => s.DocumentIds.Should().NotBeEmpty());
    }

    [Fact]
    public async Task GetChatSessions_WhenNoSessions_ReturnsEmptyList()
    {
        var handler = new GetChatSessionsQueryHandler(_chatRepo, _userMock.Object);
        var result = await handler.Handle(new GetChatSessionsQuery(), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task GetChatSessions_TitleIsFirstUserMessage()
    {
        var session = CreateSession(_userId);
        session.AddMessage(ChatRole.User, "What is the contract term?");
        session.AddMessage(ChatRole.Assistant, "It is 12 months.");
        await AddSessionAsync(session);

        var handler = new GetChatSessionsQueryHandler(_chatRepo, _userMock.Object);
        var result = await handler.Handle(new GetChatSessionsQuery(), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value[0].Title.Should().Be("What is the contract term?");
        result.Value[0].MessageCount.Should().Be(2);
    }

    // ── GetChatSession (single) ──────────────────────────────────────────────

    [Fact]
    public async Task GetChatSession_WithValidOwnedSession_ReturnsDetailWithMessages()
    {
        var session = CreateSession(_userId);
        session.AddMessage(ChatRole.User, "Question 1");
        session.AddMessage(ChatRole.Assistant, "Answer 1");
        await AddSessionAsync(session);

        var handler = new GetChatSessionQueryHandler(_chatRepo, _userMock.Object);
        var result = await handler.Handle(new GetChatSessionQuery(session.Id), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(session.Id);
        result.Value.Messages.Should().HaveCount(2);
        result.Value.Messages[0].Ordinal.Should().Be(0);
        result.Value.Messages[0].Role.Should().Be("User");
        result.Value.Messages[0].Content.Should().Be("Question 1");
        result.Value.Messages[1].Ordinal.Should().Be(1);
        result.Value.Messages[1].Role.Should().Be("Assistant");
    }

    [Fact]
    public async Task GetChatSession_WithUnknownId_ReturnsSessionNotFoundError()
    {
        var handler = new GetChatSessionQueryHandler(_chatRepo, _userMock.Object);
        var result = await handler.Handle(new GetChatSessionQuery(Guid.NewGuid()), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Chat.SessionNotFound");
    }

    [Fact]
    public async Task GetChatSession_ForeignSession_ReturnsSessionNotFoundError()
    {
        // Session belongs to another user
        var foreignSession = CreateSession(_otherUserId);
        await AddSessionAsync(foreignSession);

        var handler = new GetChatSessionQueryHandler(_chatRepo, _userMock.Object);
        var result = await handler.Handle(new GetChatSessionQuery(foreignSession.Id), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Chat.SessionNotFound");
    }

    // ── DeleteChatSession ────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteChatSession_WithValidOwnedSession_RemovesAndSucceeds()
    {
        var session = CreateSession(_userId);
        await AddSessionAsync(session);

        var handler = new DeleteChatSessionCommandHandler(_uow, _userMock.Object);
        var result = await handler.Handle(new DeleteChatSessionCommand(session.Id), default);

        result.IsSuccess.Should().BeTrue();
        var remaining = await Uow.Repository<ChatSession>().GetAllAsync();
        remaining.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteChatSession_WithUnknownId_ReturnsSessionNotFoundError()
    {
        var handler = new DeleteChatSessionCommandHandler(_uow, _userMock.Object);
        var result = await handler.Handle(new DeleteChatSessionCommand(Guid.NewGuid()), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Chat.SessionNotFound");
    }

    [Fact]
    public async Task DeleteChatSession_ForeignSession_ReturnsSessionNotFoundError()
    {
        var foreignSession = CreateSession(_otherUserId);
        await AddSessionAsync(foreignSession);

        var handler = new DeleteChatSessionCommandHandler(_uow, _userMock.Object);
        var result = await handler.Handle(new DeleteChatSessionCommand(foreignSession.Id), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Chat.SessionNotFound");

        // Session should not have been deleted
        var remaining = await Uow.Repository<ChatSession>().GetAllAsync();
        remaining.Should().HaveCount(1);
    }

    // ── Helper ───────────────────────────────────────────────────────────────

    private static ChatSession CreateSession(Guid ownerId)
    {
        var result = ChatSession.Create(ownerId, [Guid.NewGuid()]);
        result.IsSuccess.Should().BeTrue();
        return result.Value;
    }
}
