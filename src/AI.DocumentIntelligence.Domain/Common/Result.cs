namespace AI.DocumentIntelligence.Domain.Common;

/// <summary>
/// Represents the outcome of an operation that can succeed or fail with an <see cref="Error"/>,
/// without throwing. The basis of the platform's Result pattern for expected failures.
/// </summary>
public class Result
{
    /// <summary>Initializes a new <see cref="Result"/>.</summary>
    /// <param name="isSuccess">Whether the operation succeeded.</param>
    /// <param name="error">The error for a failure, or <see cref="Error.None"/> for a success.</param>
    /// <exception cref="ArgumentException">The success/error combination is inconsistent.</exception>
    protected Result(bool isSuccess, Error error)
    {
        switch (isSuccess)
        {
            case true when error != Error.None:
                throw new ArgumentException("A successful result cannot carry an error.", nameof(error));
            case false when error == Error.None:
                throw new ArgumentException("A failed result must carry an error.", nameof(error));
            default:
                IsSuccess = isSuccess;
                Error = error;
                break;
        }
    }

    /// <summary>Whether the operation succeeded.</summary>
    public bool IsSuccess { get; }

    /// <summary>Whether the operation failed.</summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>The error describing the failure, or <see cref="Error.None"/> when successful.</summary>
    public Error Error { get; }

    /// <summary>Creates a successful result.</summary>
    public static Result Success() => new(true, Error.None);

    /// <summary>Creates a failed result carrying the supplied <paramref name="error"/>.</summary>
    public static Result Failure(Error error) => new(false, error);

    /// <summary>Creates a successful result carrying <paramref name="value"/>.</summary>
    public static Result<TValue> Success<TValue>(TValue value) => new(value, true, Error.None);

    /// <summary>Creates a failed typed result carrying the supplied <paramref name="error"/>.</summary>
    public static Result<TValue> Failure<TValue>(Error error) => new(default, false, error);
}
