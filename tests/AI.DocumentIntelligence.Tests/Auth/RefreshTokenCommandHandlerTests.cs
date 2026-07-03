using AI.DocumentIntelligence.Application.Abstractions;
using AI.DocumentIntelligence.Application.Abstractions.Identity;
using AI.DocumentIntelligence.Application.Abstractions.Persistence;
using AI.DocumentIntelligence.Application.Features.Auth.Login;
using AI.DocumentIntelligence.Application.Features.Auth.Refresh;
using AI.DocumentIntelligence.Domain.Entities;
using AI.DocumentIntelligence.Domain.Enums;
using AI.DocumentIntelligence.Domain.Errors;
using FluentAssertions;
using Moq;

namespace AI.DocumentIntelligence.Tests.Auth;

/// <summary>Unit tests for <see cref="RefreshTokenCommandHandler"/>.</summary>
public sealed class RefreshTokenCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ITokenService> _tokenService = new();
    private readonly Mock<IAuditService> _auditService = new();

    private RefreshTokenCommandHandler CreateHandler() =>
        new(_userRepo.Object, _uow.Object, _tokenService.Object, _auditService.Object);

    private static User ActiveUserWithRefreshToken(
        string hashedToken = "valid-hash",
        DateTimeOffset? expiresAt = null)
    {
        var user = User.Create("user@example.com", "hash", "Test User", UserRole.Analyst);
        user.SetRefreshToken(hashedToken, expiresAt ?? DateTimeOffset.UtcNow.AddDays(7));
        return user;
    }

    [Fact]
    public async Task Handle_ValidRefreshToken_ReturnsNewTokenPair()
    {
        // Arrange
        const string incoming = "raw-token";
        const string incomingHash = "valid-hash";
        var user = ActiveUserWithRefreshToken(incomingHash);

        _tokenService.Setup(t => t.HashToken(incoming)).Returns(incomingHash);
        _userRepo.Setup(r => r.GetByRefreshTokenHashAsync(incomingHash, default)).ReturnsAsync(user);
        _tokenService.Setup(t => t.GenerateAccessToken(user)).Returns("new-access");
        _tokenService.Setup(t => t.GenerateRefreshToken()).Returns("new-refresh");
        _tokenService.Setup(t => t.HashToken("new-refresh")).Returns("new-hash");
        _tokenService.Setup(t => t.AccessTokenExpiry).Returns(TimeSpan.FromMinutes(15));
        _tokenService.Setup(t => t.RefreshTokenExpiry).Returns(TimeSpan.FromDays(7));
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        // Act
        var result = await CreateHandler().Handle(new RefreshTokenCommand(incoming), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().Be("new-access");
        result.Value.RefreshToken.Should().Be("new-refresh");
    }

    [Fact]
    public async Task Handle_UnknownToken_ReturnsInvalid()
    {
        _tokenService.Setup(t => t.HashToken(It.IsAny<string>())).Returns("unknown-hash");
        _userRepo.Setup(r => r.GetByRefreshTokenHashAsync("unknown-hash", default))
            .ReturnsAsync((User?)null);

        var result = await CreateHandler().Handle(new RefreshTokenCommand("bad-token"), default);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be(DomainErrors.Token.Invalid.Code);
    }

    [Fact]
    public async Task Handle_ExpiredRefreshToken_ReturnsInvalid()
    {
        const string incoming = "raw-token";
        const string incomingHash = "expired-hash";
        // Token expired one second ago
        var user = ActiveUserWithRefreshToken(incomingHash, DateTimeOffset.UtcNow.AddSeconds(-1));

        _tokenService.Setup(t => t.HashToken(incoming)).Returns(incomingHash);
        _userRepo.Setup(r => r.GetByRefreshTokenHashAsync(incomingHash, default)).ReturnsAsync(user);

        var result = await CreateHandler().Handle(new RefreshTokenCommand(incoming), default);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be(DomainErrors.Token.Invalid.Code);
    }

    [Fact]
    public async Task Handle_InactiveUser_ReturnsInactive()
    {
        const string incoming = "raw-token";
        const string incomingHash = "valid-hash";
        var user = ActiveUserWithRefreshToken(incomingHash);
        user.Deactivate();

        _tokenService.Setup(t => t.HashToken(incoming)).Returns(incomingHash);
        _userRepo.Setup(r => r.GetByRefreshTokenHashAsync(incomingHash, default)).ReturnsAsync(user);

        var result = await CreateHandler().Handle(new RefreshTokenCommand(incoming), default);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be(DomainErrors.User.Inactive.Code);
    }

    [Fact]
    public async Task Handle_ValidToken_AuditIsLogged()
    {
        const string incoming = "raw-token";
        const string incomingHash = "valid-hash";
        var user = ActiveUserWithRefreshToken(incomingHash);

        _tokenService.Setup(t => t.HashToken(incoming)).Returns(incomingHash);
        _userRepo.Setup(r => r.GetByRefreshTokenHashAsync(incomingHash, default)).ReturnsAsync(user);
        _tokenService.Setup(t => t.GenerateAccessToken(user)).Returns("new-access");
        _tokenService.Setup(t => t.GenerateRefreshToken()).Returns("new-refresh");
        _tokenService.Setup(t => t.HashToken("new-refresh")).Returns("new-hash");
        _tokenService.Setup(t => t.AccessTokenExpiry).Returns(TimeSpan.FromMinutes(15));
        _tokenService.Setup(t => t.RefreshTokenExpiry).Returns(TimeSpan.FromDays(7));
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        await CreateHandler().Handle(new RefreshTokenCommand(incoming), default);

        _auditService.Verify(a => a.LogAsync(
            "User.TokenRefreshed", "User", user.Id, null, default), Times.Once);
    }

    [Fact]
    public async Task Handle_ValidToken_TokenIsRotated()
    {
        // After a successful refresh, the user's stored token should change.
        const string incoming = "raw-token";
        const string incomingHash = "valid-hash";
        var user = ActiveUserWithRefreshToken(incomingHash);

        _tokenService.Setup(t => t.HashToken(incoming)).Returns(incomingHash);
        _userRepo.Setup(r => r.GetByRefreshTokenHashAsync(incomingHash, default)).ReturnsAsync(user);
        _tokenService.Setup(t => t.GenerateAccessToken(user)).Returns("a");
        _tokenService.Setup(t => t.GenerateRefreshToken()).Returns("new-refresh");
        _tokenService.Setup(t => t.HashToken("new-refresh")).Returns("new-hash");
        _tokenService.Setup(t => t.AccessTokenExpiry).Returns(TimeSpan.FromMinutes(15));
        _tokenService.Setup(t => t.RefreshTokenExpiry).Returns(TimeSpan.FromDays(7));
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        await CreateHandler().Handle(new RefreshTokenCommand(incoming), default);

        user.RefreshTokenHash.Should().Be("new-hash", "the token must be rotated on every successful refresh");
    }
}
