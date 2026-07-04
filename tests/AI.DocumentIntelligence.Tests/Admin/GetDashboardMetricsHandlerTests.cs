using AI.DocumentIntelligence.Application.Abstractions.Identity;
using AI.DocumentIntelligence.Application.Abstractions.Persistence;
using AI.DocumentIntelligence.Application.Features.Admin.GetDashboardMetrics;
using AI.DocumentIntelligence.Domain.Entities;
using AI.DocumentIntelligence.Domain.Enums;
using AI.DocumentIntelligence.Domain.ValueObjects;
using AI.DocumentIntelligence.Tests.Integration.Fakes;
using FluentAssertions;
using Moq;

namespace AI.DocumentIntelligence.Tests.Admin;

/// <summary>Unit tests for <see cref="GetDashboardMetricsQueryHandler"/> metrics aggregation.</summary>
public sealed class GetDashboardMetricsHandlerTests
{
    private readonly InMemoryUnitOfWork _uow = new();
    private readonly Mock<IUserRepository> _userRepoMock = new();

    private GetDashboardMetricsQueryHandler CreateHandler() =>
        new(_uow, _userRepoMock.Object);

    private IUnitOfWork Uow => _uow;

    [Fact]
    public async Task Handle_EmptyDatabase_ReturnsZeroCounts()
    {
        _userRepoMock.Setup(r => r.GetAllAsync(default))
            .ReturnsAsync(new List<User>().AsReadOnly());

        var result = await CreateHandler().Handle(
            new GetDashboardMetricsQuery(null, null, null, null),
            default);

        result.IsSuccess.Should().BeTrue();
        var dto = result.Value;
        dto.TotalUsers.Should().Be(0);
        dto.TotalDocuments.Should().Be(0);
        dto.TotalAnalyses.Should().Be(0);
        dto.TotalComparisons.Should().Be(0);
        dto.TotalChatSessions.Should().Be(0);
        dto.AiUsage.TotalPromptTokens.Should().Be(0);
        dto.AiUsage.TotalCompletionTokens.Should().Be(0);
        dto.AiUsage.TotalCost.Should().Be(0);
        dto.AiUsage.AverageProcessingTimeMs.Should().Be(0);
        dto.AiUsage.DailyUsage.Should().BeEmpty();
        dto.AiUsage.UsageByType.Should().BeEmpty();
        dto.RecentActivity.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithAnalysisSession_IncrementsAnalysisCount()
    {
        _userRepoMock.Setup(r => r.GetAllAsync(default))
            .ReturnsAsync(new List<User>().AsReadOnly());

        var sessionResult = AnalysisSession.Create(
            Guid.NewGuid(), [Guid.NewGuid()], AnalysisCapability.ExecutiveSummary);
        sessionResult.IsSuccess.Should().BeTrue();

        await Uow.Repository<AnalysisSession>().AddAsync(sessionResult.Value);

        var result = await CreateHandler().Handle(
            new GetDashboardMetricsQuery(null, null, null, null),
            default);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalAnalyses.Should().Be(1);
        result.Value.TotalComparisons.Should().Be(0);
        result.Value.TotalChatSessions.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WithAiUsageMetric_AggregatesTokensAndCost()
    {
        _userRepoMock.Setup(r => r.GetAllAsync(default))
            .ReturnsAsync(new List<User>().AsReadOnly());

        var metric1 = AiUsageMetric.Create(
            Guid.NewGuid(),
            "Analysis",
            new TokenUsage(100, 50, 0.01m),
            TimeSpan.FromMilliseconds(500));

        var metric2 = AiUsageMetric.Create(
            Guid.NewGuid(),
            "Chat",
            new TokenUsage(200, 100, 0.02m),
            TimeSpan.FromMilliseconds(1000));

        await Uow.Repository<AiUsageMetric>().AddAsync(metric1);
        await Uow.Repository<AiUsageMetric>().AddAsync(metric2);

        var result = await CreateHandler().Handle(
            new GetDashboardMetricsQuery(null, null, null, null),
            default);

        result.IsSuccess.Should().BeTrue();
        result.Value.AiUsage.TotalPromptTokens.Should().Be(300);
        result.Value.AiUsage.TotalCompletionTokens.Should().Be(150);
        result.Value.AiUsage.TotalCost.Should().Be(0.03m);
        result.Value.AiUsage.AverageProcessingTimeMs.Should().BeApproximately(750.0, 1.0);
    }

    [Fact]
    public async Task Handle_WithOperationTypeFilter_OnlyIncludesMatchingMetrics()
    {
        _userRepoMock.Setup(r => r.GetAllAsync(default))
            .ReturnsAsync(new List<User>().AsReadOnly());

        var analysisMetric = AiUsageMetric.Create(
            Guid.NewGuid(),
            "Analysis",
            new TokenUsage(100, 50, 0.01m),
            TimeSpan.FromMilliseconds(500));

        var chatMetric = AiUsageMetric.Create(
            Guid.NewGuid(),
            "Chat",
            new TokenUsage(200, 100, 0.02m),
            TimeSpan.FromMilliseconds(1000));

        await Uow.Repository<AiUsageMetric>().AddAsync(analysisMetric);
        await Uow.Repository<AiUsageMetric>().AddAsync(chatMetric);

        var result = await CreateHandler().Handle(
            new GetDashboardMetricsQuery(null, null, "Analysis", null),
            default);

        result.IsSuccess.Should().BeTrue();
        result.Value.AiUsage.TotalPromptTokens.Should().Be(100);
        result.Value.AiUsage.TotalCompletionTokens.Should().Be(50);
    }

    [Fact]
    public async Task Handle_UsageByType_GroupsCorrectlyWithLowercaseType()
    {
        _userRepoMock.Setup(r => r.GetAllAsync(default))
            .ReturnsAsync(new List<User>().AsReadOnly());

        for (int i = 0; i < 3; i++)
        {
            await Uow.Repository<AiUsageMetric>().AddAsync(
                AiUsageMetric.Create(
                    Guid.NewGuid(),
                    "Analysis",
                    new TokenUsage(10, 5, 0.001m),
                    TimeSpan.FromMilliseconds(100)));
        }

        await Uow.Repository<AiUsageMetric>().AddAsync(
            AiUsageMetric.Create(
                Guid.NewGuid(),
                "Chat",
                new TokenUsage(20, 10, 0.002m),
                TimeSpan.FromMilliseconds(200)));

        var result = await CreateHandler().Handle(
            new GetDashboardMetricsQuery(null, null, null, null),
            default);

        result.IsSuccess.Should().BeTrue();
        var byType = result.Value.AiUsage.UsageByType;
        byType.Should().HaveCount(2);

        var analysisGroup = byType.First(u => u.Type == "analysis");
        analysisGroup.Count.Should().Be(3);
        analysisGroup.PromptTokens.Should().Be(30);

        var chatGroup = byType.First(u => u.Type == "chat");
        chatGroup.Count.Should().Be(1);
    }

    [Fact]
    public async Task Handle_RecentActivity_LimitedToFifteenItems()
    {
        _userRepoMock.Setup(r => r.GetAllAsync(default))
            .ReturnsAsync(new List<User>().AsReadOnly());

        // Add 20 analysis sessions — only latest 15 should appear in activity
        for (int i = 0; i < 20; i++)
        {
            var sessionResult = AnalysisSession.Create(
                Guid.NewGuid(), [Guid.NewGuid()], AnalysisCapability.KeyInsights);
            await Uow.Repository<AnalysisSession>().AddAsync(sessionResult.Value);
        }

        var result = await CreateHandler().Handle(
            new GetDashboardMetricsQuery(null, null, null, null),
            default);

        result.IsSuccess.Should().BeTrue();
        result.Value.RecentActivity.Should().HaveCount(15);
    }
}
