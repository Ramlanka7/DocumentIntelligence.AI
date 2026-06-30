using AI.DocumentIntelligence.Domain.Common;
using MediatR;

namespace AI.DocumentIntelligence.Application.Common.Messaging;

/// <summary>Marker for a MediatR handler that processes an <see cref="IQuery{TResponse}"/>.</summary>
public interface IQueryHandler<TRequest, TResponse> : IRequestHandler<TRequest, Result<TResponse>>
    where TRequest : IQuery<TResponse>;
