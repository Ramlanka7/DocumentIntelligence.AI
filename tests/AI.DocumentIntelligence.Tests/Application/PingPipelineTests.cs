using AI.DocumentIntelligence.Application;
using AI.DocumentIntelligence.Application.Features.Diagnostics.Ping;
using AI.DocumentIntelligence.Domain.Common;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AI.DocumentIntelligence.Tests.Application;

/// <summary>
/// Exercises the Application CQRS backbone end to end: a request routed through MediatR and the
/// registered pipeline behaviors must return a <see cref="Result{T}"/>, succeeding for valid input
/// and short-circuiting to a validation failure for invalid input.
/// </summary>
public sealed class PingPipelineTests
{
    private static ISender BuildSender()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        return services.BuildServiceProvider().GetRequiredService<ISender>();
    }

    [Fact]
    public async Task PingQuery_WithValidMessage_FlowsThroughPipeline_AndReturnsSuccessResult()
    {
        ISender sender = BuildSender();

        Result<PingResponse> result = await sender.Send(new PingQuery("hello"));

        Assert.True(result.IsSuccess);
        Assert.Equal("Pong: hello", result.Value.Reply);
    }

    [Fact]
    public async Task PingQuery_WithEmptyMessage_IsShortCircuited_ByValidationBehavior()
    {
        ISender sender = BuildSender();

        Result<PingResponse> result = await sender.Send(new PingQuery(string.Empty));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        ValidationError validationError = Assert.IsType<ValidationError>(result.Error);
        Assert.NotEmpty(validationError.Errors);
    }
}
