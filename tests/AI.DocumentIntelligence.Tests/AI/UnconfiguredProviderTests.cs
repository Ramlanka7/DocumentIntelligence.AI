using AI.DocumentIntelligence.Application.Contracts.AI;
using AI.DocumentIntelligence.Infrastructure.AI.Providers;
using FluentAssertions;

namespace AI.DocumentIntelligence.Tests.AI;

/// <summary>
/// Verifies that providers still registered as stubs (OpenAI, Ollama) return
/// a well-typed failure rather than throwing. AnthropicProvider is fully
/// implemented and tested separately via integration tests.
/// </summary>
public sealed class UnconfiguredProviderTests
{
    private static readonly AiCompletionRequest DummyRequest =
        new([new AiMessage(AiRole.User, "hello")]);

    [Fact]
    public void OpenAiProvider_Name_ShouldBeOpenAI()
    {
        new OpenAiProvider().Name.Should().Be("OpenAI");
    }

    [Fact]
    public async Task OpenAiProvider_CompleteAsync_ShouldReturnFailure()
    {
        var result = await new OpenAiProvider().CompleteAsync(DummyRequest);
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("OpenAI.NotConfigured");
    }

    [Fact]
    public void OllamaProvider_Name_ShouldBeOllama()
    {
        new OllamaProvider().Name.Should().Be("Ollama");
    }

    [Fact]
    public async Task OllamaProvider_CompleteAsync_ShouldReturnFailure()
    {
        var result = await new OllamaProvider().CompleteAsync(DummyRequest);
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Ollama.NotConfigured");
    }
}
