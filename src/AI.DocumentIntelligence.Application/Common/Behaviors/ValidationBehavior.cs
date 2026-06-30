using AI.DocumentIntelligence.Domain.Common;
using FluentValidation;
using MediatR;

namespace AI.DocumentIntelligence.Application.Common.Behaviors;

/// <summary>
/// MediatR pipeline behavior that runs all registered FluentValidation validators for a request
/// and short-circuits with a failed <see cref="Result"/> (carrying a <see cref="ValidationError"/>)
/// when any rule fails, so invalid input never reaches a handler and no exception is thrown.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type, constrained to <see cref="Result"/>.</typeparam>
public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : Result
{
    /// <inheritdoc />
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!validators.Any())
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);

        var validationResults = await Task.WhenAll(
            validators.Select(validator => validator.ValidateAsync(context, cancellationToken)));

        Error[] errors = validationResults
            .SelectMany(result => result.Errors)
            .Where(failure => failure is not null)
            .Select(failure => Error.Validation(failure.PropertyName, failure.ErrorMessage))
            .ToArray();

        if (errors.Length == 0)
        {
            return await next();
        }

        return ResultFactory.Failure<TResponse>(new ValidationError(errors));
    }
}
