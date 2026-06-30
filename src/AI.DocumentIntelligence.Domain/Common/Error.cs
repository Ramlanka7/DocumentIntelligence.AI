using System.Diagnostics.CodeAnalysis;

namespace AI.DocumentIntelligence.Domain.Common;

/// <summary>
/// Represents an expected failure as a value (code, human-readable description and
/// <see cref="ErrorType"/>) rather than an exception. Supports the platform rule that
/// exceptions are never used for control flow.
/// </summary>
/// <param name="Code">A stable, machine-readable identifier such as <c>"Document.NotFound"</c>.</param>
/// <param name="Description">A human-readable explanation of the failure.</param>
/// <param name="Type">The category of the error.</param>
[SuppressMessage(
    "Naming",
    "CA1716:Identifiers should not match keywords",
    Justification = "'Error' is the idiomatic name for the Result pattern's failure value and is used throughout the domain.")]
public record Error(string Code, string Description, ErrorType Type)
{
    /// <summary>The canonical "no error" value carried by a successful <see cref="Result"/>.</summary>
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.Failure);

    /// <summary>Returned when a non-null value was required but <see langword="null"/> was supplied.</summary>
    public static readonly Error NullValue =
        new("Error.NullValue", "A null value was provided.", ErrorType.Failure);

    /// <summary>Creates a generic <see cref="ErrorType.Failure"/> error.</summary>
    public static Error Failure(string code, string description) => new(code, description, ErrorType.Failure);

    /// <summary>Creates an <see cref="ErrorType.Validation"/> error.</summary>
    public static Error Validation(string code, string description) => new(code, description, ErrorType.Validation);

    /// <summary>Creates an <see cref="ErrorType.NotFound"/> error.</summary>
    public static Error NotFound(string code, string description) => new(code, description, ErrorType.NotFound);

    /// <summary>Creates an <see cref="ErrorType.Conflict"/> error.</summary>
    public static Error Conflict(string code, string description) => new(code, description, ErrorType.Conflict);

    /// <summary>Creates an <see cref="ErrorType.Unauthorized"/> error.</summary>
    public static Error Unauthorized(string code, string description) => new(code, description, ErrorType.Unauthorized);

    /// <summary>Creates an <see cref="ErrorType.Forbidden"/> error.</summary>
    public static Error Forbidden(string code, string description) => new(code, description, ErrorType.Forbidden);
}
