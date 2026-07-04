using AI.DocumentIntelligence.Application.Abstractions.Identity;
using AI.DocumentIntelligence.Application.Abstractions.Persistence;
using AI.DocumentIntelligence.Application.Features.Comparison.GetComparisonSessions;
using AI.DocumentIntelligence.Domain.Entities;
using AI.DocumentIntelligence.Domain.Enums;
using AI.DocumentIntelligence.Tests.Integration.Fakes;
using FluentAssertions;
using Moq;

namespace AI.DocumentIntelligence.Tests.Comparison;

/// <summary>Unit tests for <see cref="GetComparisonSessionsQueryHandler"/> owner-scoping.</summary>
public sealed class GetComparisonSessionsHandlerTests
{
    private readonly InMemoryUnitOfWork _uow = new();
    private readonly Mock<ICurrentUser> _userMock = new();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _otherUserId = Guid.NewGuid();

    private IUnitOfWork Uow => _uow;

    public GetComparisonSessionsHandlerTests()
    {
        _userMock.Setup(u => u.UserId).Returns(_userId);
    }

    [Fact]
    public async Task Handle_ReturnsOnlyCurrentUserSessions()
    {
        var owned = CreateSession(_userId);
        var foreign = CreateSession(_otherUserId);

        await Uow.Repository<ComparisonSession>().AddAsync(owned);
        await Uow.Repository<ComparisonSession>().AddAsync(foreign);

        var handler = new GetComparisonSessionsQueryHandler(_uow, _userMock.Object);
        var result = await handler.Handle(new GetComparisonSessionsQuery(), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value[0].Id.Should().Be(owned.Id);
    }

    [Fact]
    public async Task Handle_MapsComparisonTypeAndStatusCorrectly()
    {
        var session = CreateSession(_userId, ComparisonType.Contract);
        await Uow.Repository<ComparisonSession>().AddAsync(session);

        var handler = new GetComparisonSessionsQueryHandler(_uow, _userMock.Object);
        var result = await handler.Handle(new GetComparisonSessionsQuery(), default);

        result.IsSuccess.Should().BeTrue();
        result.Value[0].ComparisonType.Should().Be("Contract");
        result.Value[0].Status.Should().Be("Pending");
    }

    [Fact]
    public async Task Handle_WhenNoSessions_ReturnsEmptyList()
    {
        var handler = new GetComparisonSessionsQueryHandler(_uow, _userMock.Object);
        var result = await handler.Handle(new GetComparisonSessionsQuery(), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    private static ComparisonSession CreateSession(Guid ownerId, ComparisonType type = ComparisonType.SideBySide)
    {
        var result = ComparisonSession.Create(ownerId, [Guid.NewGuid(), Guid.NewGuid()], type);
        result.IsSuccess.Should().BeTrue();
        return result.Value;
    }
}
