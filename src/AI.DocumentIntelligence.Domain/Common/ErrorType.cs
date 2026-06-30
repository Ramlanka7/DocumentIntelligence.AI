namespace AI.DocumentIntelligence.Domain.Common;

/// <summary>
/// Classifies an <see cref="Error"/> so that outer layers (e.g. the API) can map a
/// failed <see cref="Result"/> onto an appropriate transport response without inspecting
/// error codes.
/// </summary>
public enum ErrorType
{
    /// <summary>An unexpected or generic failure.</summary>
    Failure = 0,

    /// <summary>Input failed validation rules.</summary>
    Validation = 1,

    /// <summary>A requested resource could not be found.</summary>
    NotFound = 2,

    /// <summary>The operation conflicts with the current state of a resource.</summary>
    Conflict = 3,

    /// <summary>The caller is not authenticated.</summary>
    Unauthorized = 4,

    /// <summary>The caller is authenticated but not permitted to perform the operation.</summary>
    Forbidden = 5,
}
