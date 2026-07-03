using AI.DocumentIntelligence.Application.Abstractions;
using AI.DocumentIntelligence.Application.Abstractions.Identity;
using AI.DocumentIntelligence.Application.Abstractions.Persistence;
using AI.DocumentIntelligence.Application.Features.Auth.Register;
using AI.DocumentIntelligence.Domain.Entities;
using AI.DocumentIntelligence.Domain.Enums;
using AI.DocumentIntelligence.Domain.Errors;
using FluentAssertions;
using Moq;

namespace AI.DocumentIntelligence.Tests.Auth;

/// <summary>Unit tests for <see cref="RegisterUserCommandHandler"/>.</summary>
public sealed class RegisterUserCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<IAuditService> _auditService = new();

    private RegisterUserCommandHandler CreateHandler() =>
        new(_userRepo.Object, _uow.Object, _passwordHasher.Object, _auditService.Object);

    private static RegisterUserCommand ValidCommand(string email = "new@example.com") =>
        new(email, "Secure@123", "New User", UserRole.Analyst);

    [Fact]
    public async Task Handle_NewEmail_CreatesUserAndReturnsId()
    {
        // Arrange: no existing user with this email
        _userRepo.Setup(r => r.GetByEmailAsync("new@example.com", default))
            .ReturnsAsync((User?)null);
        _passwordHasher.Setup(p => p.Hash("Secure@123")).Returns("hashed");
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        // Act
        var result = await CreateHandler().Handle(ValidCommand(), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task Handle_DuplicateEmail_ReturnsEmailAlreadyInUse()
    {
        var existing = User.Create("new@example.com", "hash", "Existing", UserRole.Analyst);
        _userRepo.Setup(r => r.GetByEmailAsync("new@example.com", default))
            .ReturnsAsync(existing);

        var result = await CreateHandler().Handle(ValidCommand(), default);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be(DomainErrors.User.EmailAlreadyInUse.Code);
    }

    [Fact]
    public async Task Handle_NewUser_PasswordIsHashed()
    {
        _userRepo.Setup(r => r.GetByEmailAsync("new@example.com", default))
            .ReturnsAsync((User?)null);
        _passwordHasher.Setup(p => p.Hash("Secure@123")).Returns("bcrypt-hash");
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        await CreateHandler().Handle(ValidCommand(), default);

        _passwordHasher.Verify(p => p.Hash("Secure@123"), Times.Once);
    }

    [Fact]
    public async Task Handle_Success_AuditIsLogged()
    {
        _userRepo.Setup(r => r.GetByEmailAsync("new@example.com", default))
            .ReturnsAsync((User?)null);
        _passwordHasher.Setup(p => p.Hash(It.IsAny<string>())).Returns("hash");
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        await CreateHandler().Handle(ValidCommand(), default);

        _auditService.Verify(a => a.LogAsync(
            "User.Registered", "User", It.IsAny<Guid?>(), null, default), Times.Once);
    }

    [Fact]
    public async Task Handle_Success_SaveChangesCalledOnce()
    {
        _userRepo.Setup(r => r.GetByEmailAsync("new@example.com", default))
            .ReturnsAsync((User?)null);
        _passwordHasher.Setup(p => p.Hash(It.IsAny<string>())).Returns("hash");
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        await CreateHandler().Handle(ValidCommand(), default);

        _uow.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Handle_EmailIsCaseFolded()
    {
        // The handler lower-cases the email before lookup and storage.
        _userRepo.Setup(r => r.GetByEmailAsync("mixed@example.com", default))
            .ReturnsAsync((User?)null);
        _passwordHasher.Setup(p => p.Hash(It.IsAny<string>())).Returns("hash");
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        var cmd = new RegisterUserCommand("Mixed@EXAMPLE.COM", "Secure@123", "User", UserRole.Analyst);
        var result = await CreateHandler().Handle(cmd, default);

        result.IsSuccess.Should().BeTrue();
        // Lookup was performed with the normalised email
        _userRepo.Verify(r => r.GetByEmailAsync("mixed@example.com", default), Times.Once);
    }
}
