using System.Reflection;
using AI.DocumentIntelligence.Domain.Common;

namespace AI.DocumentIntelligence.Application.Common.Behaviors;

/// <summary>
/// Builds a failed <see cref="Result"/> or <see cref="Result{TValue}"/> for an arbitrary pipeline
/// response type. Pipeline behaviors are generic over <c>TResponse</c> and cannot construct the
/// concrete (possibly generic) result directly, so this resolves the correct factory by reflection.
/// </summary>
internal static class ResultFactory
{
    /// <summary>
    /// Creates a failed <typeparamref name="TResponse"/> carrying <paramref name="error"/>.
    /// </summary>
    /// <typeparam name="TResponse">A <see cref="Result"/> or closed <see cref="Result{TValue}"/>.</typeparam>
    /// <param name="error">The error the failed result should carry.</param>
    public static TResponse Failure<TResponse>(Error error)
        where TResponse : Result
    {
        if (typeof(TResponse) == typeof(Result))
        {
            return (TResponse)(object)Result.Failure(error);
        }

        // TResponse is a closed Result<TValue>; build it via the generic base factory
        // Result.Failure<TValue>(Error), disambiguated from the non-generic overload by arity.
        Type valueType = typeof(TResponse).GetGenericArguments()[0];

        MethodInfo failure = typeof(Result).GetMethod(
            nameof(Result.Failure),
            genericParameterCount: 1,
            BindingFlags.Static | BindingFlags.Public,
            binder: null,
            types: [typeof(Error)],
            modifiers: null)!;

        return (TResponse)failure.MakeGenericMethod(valueType).Invoke(null, [error])!;
    }
}
