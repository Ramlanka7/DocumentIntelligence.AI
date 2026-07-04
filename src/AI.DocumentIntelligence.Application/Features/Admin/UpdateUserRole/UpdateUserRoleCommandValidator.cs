using FluentValidation;

namespace AI.DocumentIntelligence.Application.Features.Admin.UpdateUserRole;

/// <summary>Validates <see cref="UpdateUserRoleCommand"/>.</summary>
internal sealed class UpdateUserRoleCommandValidator : AbstractValidator<UpdateUserRoleCommand>
{
    private static readonly string[] ValidRoles = ["Admin", "Analyst", "Viewer"];

    public UpdateUserRoleCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("User ID is required.");

        RuleFor(x => x.Role)
            .NotEmpty().WithMessage("Role is required.")
            .Must(r => ValidRoles.Contains(r, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Role must be one of: Admin, Analyst, Viewer.");
    }
}
