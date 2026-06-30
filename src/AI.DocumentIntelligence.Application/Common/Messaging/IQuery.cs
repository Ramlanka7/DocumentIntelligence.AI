using AI.DocumentIntelligence.Domain.Common;
using MediatR;

namespace AI.DocumentIntelligence.Application.Common.Messaging;

/// <summary>Marker for a MediatR query that returns a <see cref="Result{TResponse}"/>.</summary>
public interface IQuery<TResponse> : IRequest<Result<TResponse>>;
