using FluentValidation;

namespace AI.DocumentIntelligence.Application.Features.Auth.Logout;

/// <summary>
/// Validator for <see cref="LogoutCommand"/>. The command carries no input fields — validation
/// is satisfied by the [Authorize] attribute on the controller action that dispatches it.
/// </summary>
internal sealed class LogoutCommandValidator : AbstractValidator<LogoutCommand>;
