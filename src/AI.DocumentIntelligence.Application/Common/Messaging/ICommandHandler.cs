using AI.DocumentIntelligence.Domain.Common;
using MediatR;

namespace AI.DocumentIntelligence.Application.Common.Messaging;

/// <summary>Marker for a MediatR handler that processes an <see cref="ICommand"/>.</summary>
public interface ICommandHandler<TRequest> : IRequestHandler<TRequest, Result>
    where TRequest : ICommand;

/// <summary>Marker for a MediatR handler that processes an <see cref="ICommand{TResponse}"/>.</summary>
public interface ICommandHandler<TRequest, TResponse> : IRequestHandler<TRequest, Result<TResponse>>
    where TRequest : ICommand<TResponse>;
