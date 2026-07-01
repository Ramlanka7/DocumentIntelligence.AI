using AI.DocumentIntelligence.Application.Features.Auth.Login;
using AI.DocumentIntelligence.Application.Features.Auth.Logout;
using AI.DocumentIntelligence.Application.Features.Auth.Refresh;
using AI.DocumentIntelligence.Application.Features.Auth.Register;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace AI.DocumentIntelligence.Tests.Auth;

public sealed class CommandValidatorTests
{
    // ---- LoginCommandValidator ----

    [Fact]
    public void LoginValidator_ValidCommand_NoErrors()
    {
        var validator = new LoginCommandValidator();
        var result = validator.TestValidate(new LoginCommand("user@example.com", "password123"));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("", "pass")]
    [InlineData("not-an-email", "pass")]
    public void LoginValidator_InvalidEmail_HasError(string email, string password)
    {
        var validator = new LoginCommandValidator();
        var result = validator.TestValidate(new LoginCommand(email, password));
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void LoginValidator_EmptyPassword_HasError()
    {
        var validator = new LoginCommandValidator();
        var result = validator.TestValidate(new LoginCommand("user@example.com", ""));
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    // ---- RefreshTokenCommandValidator ----

    [Fact]
    public void RefreshValidator_ValidCommand_NoErrors()
    {
        var validator = new RefreshTokenCommandValidator();
        var result = validator.TestValidate(new RefreshTokenCommand("some-opaque-token"));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void RefreshValidator_EmptyToken_HasError()
    {
        var validator = new RefreshTokenCommandValidator();
        var result = validator.TestValidate(new RefreshTokenCommand(""));
        result.ShouldHaveValidationErrorFor(x => x.RefreshToken);
    }

    // ---- RegisterUserCommandValidator ----

    [Fact]
    public void RegisterValidator_ValidCommand_NoErrors()
    {
        var validator = new RegisterUserCommandValidator();
        var cmd = new RegisterUserCommand("user@example.com", "Secure@123", "John Doe",
            AI.DocumentIntelligence.Domain.Enums.UserRole.Analyst);
        var result = validator.TestValidate(cmd);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void RegisterValidator_ShortPassword_HasError()
    {
        var validator = new RegisterUserCommandValidator();
        var cmd = new RegisterUserCommand("user@example.com", "Ab1!", "John Doe",
            AI.DocumentIntelligence.Domain.Enums.UserRole.Analyst);
        var result = validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void RegisterValidator_PasswordMissingDigit_HasError()
    {
        var validator = new RegisterUserCommandValidator();
        var cmd = new RegisterUserCommand("user@example.com", "NoDigits!", "John Doe",
            AI.DocumentIntelligence.Domain.Enums.UserRole.Analyst);
        var result = validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void RegisterValidator_PasswordMissingSpecialChar_HasError()
    {
        var validator = new RegisterUserCommandValidator();
        var cmd = new RegisterUserCommand("user@example.com", "NoSpecial1", "John Doe",
            AI.DocumentIntelligence.Domain.Enums.UserRole.Analyst);
        var result = validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void RegisterValidator_InvalidEmail_HasError()
    {
        var validator = new RegisterUserCommandValidator();
        var cmd = new RegisterUserCommand("not-an-email", "Secure@123", "John Doe",
            AI.DocumentIntelligence.Domain.Enums.UserRole.Analyst);
        var result = validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    // ---- LogoutCommandValidator ----

    [Fact]
    public void LogoutValidator_AlwaysPasses()
    {
        var validator = new LogoutCommandValidator();
        var result = validator.TestValidate(new LogoutCommand());
        result.ShouldNotHaveAnyValidationErrors();
    }
}
