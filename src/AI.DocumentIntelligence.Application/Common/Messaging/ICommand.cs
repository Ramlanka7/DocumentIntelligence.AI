using AI.DocumentIntelligence.Domain.Common;
using MediatR;

namespace AI.DocumentIntelligence.Application.Common.Messaging;

/// <summary>Marker for a MediatR command that returns a <see cref="Result"/>.</summary>
public interface ICommand : IRequest<Result>;

/// <summary>Marker for a MediatR command that returns a <see cref="Result{TResponse}"/>.</summary>
public interface ICommand<TResponse> : IRequest<Result<TResponse>>;
