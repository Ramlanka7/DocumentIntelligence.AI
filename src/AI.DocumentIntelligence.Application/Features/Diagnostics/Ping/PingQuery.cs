using AI.DocumentIntelligence.Application.Common.Messaging;

namespace AI.DocumentIntelligence.Application.Features.Diagnostics.Ping;

/// <summary>
/// A trivial sample query used to exercise (and smoke-test) the MediatR pipeline end to end:
/// validation behavior, logging, performance timing and the <c>Result</c> return contract.
/// </summary>
/// <param name="Message">A non-empty message to echo back.</param>
public sealed record PingQuery(string Message) : IQuery<PingResponse>;
