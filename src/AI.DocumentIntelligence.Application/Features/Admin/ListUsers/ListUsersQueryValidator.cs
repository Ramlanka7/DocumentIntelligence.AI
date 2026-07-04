using FluentValidation;

namespace AI.DocumentIntelligence.Application.Features.Admin.ListUsers;

/// <summary>Validates <see cref="ListUsersQuery"/> — no additional input rules.</summary>
internal sealed class ListUsersQueryValidator : AbstractValidator<ListUsersQuery>
{
    public ListUsersQueryValidator()
    {
        // No rules needed; the query carries no input parameters.
    }
}
