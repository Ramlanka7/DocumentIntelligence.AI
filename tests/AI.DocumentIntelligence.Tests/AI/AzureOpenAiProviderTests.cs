using AI.DocumentIntelligence.Application.Contracts.AI;
using AI.DocumentIntelligence.Infrastructure.AI.Options;
using AI.DocumentIntelligence.Infrastructure.AI.Providers;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AI.DocumentIntelligence.Tests.AI;

public sealed class AzureOpenAiProviderTests
{
    [Fact]
    public void Name_ShouldBeAzureOpenAI()
    {
        var options = Options.Create(new AzureOpenAIOptions
        {
            Endpoint = "https://placeholder.openai.azure.com/",
            ApiKey = "placeholder-key",
            ChatDeployment = "gpt-4o"
        });

        var provider = new AzureOpenAiProvider(options, NullLogger<AzureOpenAiProvider>.Instance);

        provider.Name.Should().Be("AzureOpenAI");
    }

    [Fact]
    public void Constructor_WithEmptyEndpoint_ShouldThrowUriFormatException()
    {
        var options = Options.Create(new AzureOpenAIOptions
        {
            Endpoint = string.Empty,
            ApiKey = "key"
        });

        var act = () => new AzureOpenAiProvider(options, NullLogger<AzureOpenAiProvider>.Instance);

        act.Should().Throw<Exception>();
    }
}
