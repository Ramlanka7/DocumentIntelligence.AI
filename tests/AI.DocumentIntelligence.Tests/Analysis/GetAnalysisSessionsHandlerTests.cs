using AI.DocumentIntelligence.Application.Abstractions.Identity;
using AI.DocumentIntelligence.Application.Abstractions.Persistence;
using AI.DocumentIntelligence.Application.Features.Analysis.GetAnalysisSessions;
using AI.DocumentIntelligence.Domain.Entities;
using AI.DocumentIntelligence.Domain.Enums;
using AI.DocumentIntelligence.Tests.Integration.Fakes;
using FluentAssertions;
using Moq;

namespace AI.DocumentIntelligence.Tests.Analysis;

/// <summary>Unit tests for <see cref="GetAnalysisSessionsQueryHandler"/> owner-scoping.</summary>
public sealed class GetAnalysisSessionsHandlerTests
{
    private readonly InMemoryUnitOfWork _uow = new();
    private readonly Mock<ICurrentUser> _userMock = new();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _otherUserId = Guid.NewGuid();

    private IUnitOfWork Uow => _uow;

    public GetAnalysisSessionsHandlerTests()
    {
        _userMock.Setup(u => u.UserId).Returns(_userId);
    }

    [Fact]
    public async Task Handle_ReturnsOnlyCurrentUserSessions()
    {
        var owned = CreateSession(_userId);
        var foreign = CreateSession(_otherUserId);

        await Uow.Repository<AnalysisSession>().AddAsync(owned);
        await Uow.Repository<AnalysisSession>().AddAsync(foreign);

        var handler = new GetAnalysisSessionsQueryHandler(_uow, _userMock.Object);
        var result = await handler.Handle(new GetAnalysisSessionsQuery(), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value[0].Id.Should().Be(owned.Id);
    }

    [Fact]
    public async Task Handle_MapsCapabilityAndStatusCorrectly()
    {
        var session = CreateSession(_userId, AnalysisCapability.RiskAssessment);
        await Uow.Repository<AnalysisSession>().AddAsync(session);

        var handler = new GetAnalysisSessionsQueryHandler(_uow, _userMock.Object);
        var result = await handler.Handle(new GetAnalysisSessionsQuery(), default);

        result.IsSuccess.Should().BeTrue();
        result.Value[0].Capability.Should().Be("RiskAssessment");
        result.Value[0].Status.Should().Be("Pending");
    }

    [Fact]
    public async Task Handle_WhenNoSessions_ReturnsEmptyList()
    {
        var handler = new GetAnalysisSessionsQueryHandler(_uow, _userMock.Object);
        var result = await handler.Handle(new GetAnalysisSessionsQuery(), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    private static AnalysisSession CreateSession(Guid ownerId, AnalysisCapability capability = AnalysisCapability.ExecutiveSummary)
    {
        var result = AnalysisSession.Create(ownerId, [Guid.NewGuid()], capability);
        result.IsSuccess.Should().BeTrue();
        return result.Value;
    }
}
