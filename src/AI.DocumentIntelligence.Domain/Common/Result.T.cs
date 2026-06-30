namespace AI.DocumentIntelligence.Domain.Common;

/// <summary>
/// The outcome of an operation that yields a <typeparamref name="TValue"/> on success or an
/// <see cref="Error"/> on failure, without throwing. Access <see cref="Value"/> only when
/// <see cref="Result.IsSuccess"/> is <see langword="true"/>.
/// </summary>
/// <typeparam name="TValue">The type of the value produced on success.</typeparam>
public sealed class Result<TValue> : Result
{
    private readonly TValue? _value;

    /// <summary>Initializes a new typed <see cref="Result{TValue}"/>.</summary>
    /// <param name="value">The value on success, or <see langword="default"/> on failure.</param>
    /// <param name="isSuccess">Whether the operation succeeded.</param>
    /// <param name="error">The error on failure, or <see cref="Error.None"/> on success.</param>
    internal Result(TValue? value, bool isSuccess, Error error)
        : base(isSuccess, error)
    {
        _value = value;
    }

    /// <summary>The produced value.</summary>
    /// <exception cref="InvalidOperationException">The result is a failure.</exception>
    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("The value of a failed result cannot be accessed.");

    /// <summary>Implicitly wraps a value in a successful result (or a failure when <see langword="null"/>).</summary>
    public static implicit operator Result<TValue>(TValue? value) =>
        value is not null ? Success(value) : Failure<TValue>(Error.NullValue);
}
