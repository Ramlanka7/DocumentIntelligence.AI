namespace AI.DocumentIntelligence.Domain.Common;

/// <summary>
/// An <see cref="Error"/> that aggregates one or more individual validation failures, allowing a
/// single failed <see cref="Result"/> to surface every broken rule at once.
/// </summary>
/// <param name="Errors">The individual validation errors that occurred.</param>
public sealed record ValidationError(IReadOnlyList<Error> Errors)
    : Error("Validation.General", "One or more validation errors occurred.", ErrorType.Validation);
