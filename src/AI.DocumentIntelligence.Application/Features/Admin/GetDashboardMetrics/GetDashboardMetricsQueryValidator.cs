using FluentValidation;

namespace AI.DocumentIntelligence.Application.Features.Admin.GetDashboardMetrics;

/// <summary>Validates optional filter parameters on <see cref="GetDashboardMetricsQuery"/>.</summary>
internal sealed class GetDashboardMetricsQueryValidator : AbstractValidator<GetDashboardMetricsQuery>
{
    private static readonly HashSet<string> AllowedOperationTypes =
        new(StringComparer.OrdinalIgnoreCase) { "Analysis", "Comparison", "Chat" };

    public GetDashboardMetricsQueryValidator()
    {
        When(x => x.OperationType is not null, () =>
        {
            RuleFor(x => x.OperationType)
                .Must(op => AllowedOperationTypes.Contains(op!))
                .WithMessage("operationType must be one of: Analysis, Comparison, Chat.");
        });

        When(x => x.DateFrom.HasValue && x.DateTo.HasValue, () =>
        {
            RuleFor(x => x)
                .Must(x => x.DateFrom!.Value <= x.DateTo!.Value)
                .WithMessage("dateFrom must not be later than dateTo.");
        });
    }
}
