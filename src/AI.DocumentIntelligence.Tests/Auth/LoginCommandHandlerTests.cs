using AI.DocumentIntelligence.Application.Abstractions;
using AI.DocumentIntelligence.Application.Abstractions.Identity;
using AI.DocumentIntelligence.Application.Abstractions.Persistence;
using AI.DocumentIntelligence.Application.Features.Auth.Login;
using AI.DocumentIntelligence.Domain.Entities;
using AI.DocumentIntelligence.Domain.Enums;
using AI.DocumentIntelligence.Domain.Errors;
using FluentAssertions;
using Moq;

namespace AI.DocumentIntelligence.Tests.Auth;

public sealed class LoginCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ITokenService> _tokenService = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<IAuditService> _auditService = new();

    private LoginCommandHandler CreateHandler() =>
        new(_userRepo.Object, _uow.Object, _tokenService.Object,
            _passwordHasher.Object, _auditService.Object);

    private static User ActiveUser(string email = "user@example.com") =>
        User.Create(email, "hash", "Test User", UserRole.Analyst);

    [Fact]
    public async Task Handle_ValidCredentials_ReturnsTokens()
    {
        var user = ActiveUser();
        _userRepo.Setup(r => r.GetByEmailAsync("user@example.com", default))
            .ReturnsAsync(user);
        _passwordHasher.Setup(p => p.Verify("pass", "hash")).Returns(true);
        _tokenService.Setup(t => t.GenerateAccessToken(user)).Returns("access");
        _tokenService.Setup(t => t.GenerateRefreshToken()).Returns("refresh");
        _tokenService.Setup(t => t.HashToken("refresh")).Returns("hashed");
        _tokenService.Setup(t => t.AccessTokenExpiry).Returns(TimeSpan.FromMinutes(15));
        _tokenService.Setup(t => t.RefreshTokenExpiry).Returns(TimeSpan.FromDays(7));
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        var result = await CreateHandler().Handle(new LoginCommand("user@example.com", "pass"), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().Be("access");
        result.Value.RefreshToken.Should().Be("refresh");
    }

    [Fact]
    public async Task Handle_UserNotFound_ReturnsInvalidCredentials()
    {
        _userRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), default))
            .ReturnsAsync((User?)null);

        var result = await CreateHandler().Handle(new LoginCommand("x@x.com", "pass"), default);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be(DomainErrors.User.InvalidCredentials.Code);
    }

    [Fact]
    public async Task Handle_WrongPassword_ReturnsInvalidCredentials()
    {
        var user = ActiveUser();
        _userRepo.Setup(r => r.GetByEmailAsync("user@example.com", default)).ReturnsAsync(user);
        _passwordHasher.Setup(p => p.Verify("wrong", "hash")).Returns(false);

        var result = await CreateHandler().Handle(new LoginCommand("user@example.com", "wrong"), default);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be(DomainErrors.User.InvalidCredentials.Code);
    }

    [Fact]
    public async Task Handle_InactiveUser_ReturnsInactive()
    {
        var user = ActiveUser();
        user.Deactivate();
        _userRepo.Setup(r => r.GetByEmailAsync("user@example.com", default)).ReturnsAsync(user);
        _passwordHasher.Setup(p => p.Verify("pass", "hash")).Returns(true);

        var result = await CreateHandler().Handle(new LoginCommand("user@example.com", "pass"), default);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be(DomainErrors.User.Inactive.Code);
    }

    [Fact]
    public async Task Handle_ValidCredentials_AuditIsLogged()
    {
        var user = ActiveUser();
        _userRepo.Setup(r => r.GetByEmailAsync("user@example.com", default)).ReturnsAsync(user);
        _passwordHasher.Setup(p => p.Verify("pass", "hash")).Returns(true);
        _tokenService.Setup(t => t.GenerateAccessToken(user)).Returns("a");
        _tokenService.Setup(t => t.GenerateRefreshToken()).Returns("r");
        _tokenService.Setup(t => t.HashToken("r")).Returns("rh");
        _tokenService.Setup(t => t.AccessTokenExpiry).Returns(TimeSpan.FromMinutes(15));
        _tokenService.Setup(t => t.RefreshTokenExpiry).Returns(TimeSpan.FromDays(7));
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        await CreateHandler().Handle(new LoginCommand("user@example.com", "pass"), default);

        _auditService.Verify(a => a.LogAsync(
            "User.LoggedIn", "User", user.Id, null, default), Times.Once);
    }

    [Fact]
    public async Task Handle_ValidCredentials_SaveChangesCalledOnce()
    {
        var user = ActiveUser();
        _userRepo.Setup(r => r.GetByEmailAsync("user@example.com", default)).ReturnsAsync(user);
        _passwordHasher.Setup(p => p.Verify("pass", "hash")).Returns(true);
        _tokenService.Setup(t => t.GenerateAccessToken(user)).Returns("a");
        _tokenService.Setup(t => t.GenerateRefreshToken()).Returns("r");
        _tokenService.Setup(t => t.HashToken("r")).Returns("rh");
        _tokenService.Setup(t => t.AccessTokenExpiry).Returns(TimeSpan.FromMinutes(15));
        _tokenService.Setup(t => t.RefreshTokenExpiry).Returns(TimeSpan.FromDays(7));
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        await CreateHandler().Handle(new LoginCommand("user@example.com", "pass"), default);

        // Only one save — audit record enqueued before the single SaveChangesAsync call.
        _uow.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }
}
