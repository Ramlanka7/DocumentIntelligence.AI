using AI.DocumentIntelligence.Application.Abstractions.AI;
using AI.DocumentIntelligence.Application.Contracts.AI;
using AI.DocumentIntelligence.Domain.Common;
using AI.DocumentIntelligence.Infrastructure.AI.Options;
using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI.Chat;

namespace AI.DocumentIntelligence.Infrastructure.AI.Providers;

/// <summary>
/// <see cref="IAIProvider"/> implementation backed by Azure OpenAI (Foundry). This is the default
/// provider; others are selected via <see cref="AiProviderOptions.ProviderName"/>.
/// </summary>
internal sealed partial class AzureOpenAiProvider : IAIProvider
{
    /// <summary>Stable identifier used for provider selection and telemetry.</summary>
    public const string ProviderName = "AzureOpenAI";

    private readonly ChatClient _client;
    private readonly AzureOpenAIOptions _options;
    private readonly ILogger<AzureOpenAiProvider> _logger;

    /// <inheritdoc />
    public string Name => ProviderName;

    public AzureOpenAiProvider(
        IOptions<AzureOpenAIOptions> options,
        ILogger<AzureOpenAiProvider> logger)
    {
        _options = options.Value;
        _logger = logger;

        var azureClient = new AzureOpenAIClient(
            new Uri(_options.Endpoint),
            new AzureKeyCredential(_options.ApiKey));

        _client = azureClient.GetChatClient(_options.ChatDeployment);
    }

    /// <inheritdoc />
    public async Task<Result<AiCompletionResult>> CompleteAsync(
        AiCompletionRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            LogCompletionRequest(_logger, _options.ChatDeployment, request.Messages.Count);

            var messages = request.Messages.Select(ToSdkMessage).ToList();

            var completionOptions = new ChatCompletionOptions
            {
                Temperature = (float)request.Temperature
            };

            if (request.MaxTokens.HasValue)
            {
                completionOptions.MaxOutputTokenCount = request.MaxTokens.Value;
            }

            var response = await _client.CompleteChatAsync(
                messages, completionOptions, cancellationToken);

            if (response.Value.Content.Count == 0)
            {
                return Result.Failure<AiCompletionResult>(
                    Error.Failure(
                        "AzureOpenAI.EmptyResponse",
                        "Azure OpenAI returned an empty chat completion response."));
            }

            var text = string.Concat(response.Value.Content.Select(c => c.Text));
            var usage = response.Value.Usage;
            var promptTokens = usage.InputTokenCount;
            var completionTokens = usage.OutputTokenCount;
            var estimatedCost = CalculateCost(promptTokens, completionTokens);

            var tokenUsage = new TokenUsage(promptTokens, completionTokens, estimatedCost);

            LogCompletionReceived(_logger, promptTokens, completionTokens, _options.ChatDeployment);

            return Result.Success(new AiCompletionResult(text, tokenUsage, _options.ChatDeployment));
        }
        catch (RequestFailedException ex)
        {
            LogRequestFailed(_logger, ex.Status, ex.ErrorCode ?? "unknown", ex);
            return Result.Failure<AiCompletionResult>(
                Error.Failure(
                    "AzureOpenAI.RequestFailed",
                    $"Azure OpenAI completion request failed ({ex.Status}): {ex.Message}"));
        }
        catch (Exception ex)
        {
            LogUnexpectedError(_logger, ex);
            return Result.Failure<AiCompletionResult>(
                Error.Failure(
                    "AzureOpenAI.UnexpectedError",
                    $"Unexpected error during chat completion: {ex.Message}"));
        }
    }

    private static ChatMessage ToSdkMessage(AiMessage message) => message.Role switch
    {
        AiRole.System => ChatMessage.CreateSystemMessage(message.Content),
        AiRole.Assistant => ChatMessage.CreateAssistantMessage(message.Content),
        _ => ChatMessage.CreateUserMessage(message.Content)
    };

    private decimal CalculateCost(int promptTokens, int completionTokens) =>
        (promptTokens * _options.InputCostPerMillionTokens
         + completionTokens * _options.OutputCostPerMillionTokens)
        / 1_000_000m;

    [LoggerMessage(Level = LogLevel.Debug,
        Message = "Requesting chat completion via deployment '{Deployment}' with {MessageCount} message(s)")]
    private static partial void LogCompletionRequest(
        ILogger logger, string deployment, int messageCount);

    [LoggerMessage(Level = LogLevel.Debug,
        Message = "Chat completion received — {PromptTokens} prompt, {CompletionTokens} completion tokens via '{Model}'")]
    private static partial void LogCompletionReceived(
        ILogger logger, int promptTokens, int completionTokens, string model);

    [LoggerMessage(Level = LogLevel.Error,
        Message = "Azure OpenAI completion request failed: status={Status} code={ErrorCode}")]
    private static partial void LogRequestFailed(
        ILogger logger, int status, string errorCode, Exception exception);

    [LoggerMessage(Level = LogLevel.Error,
        Message = "Unexpected error during Azure OpenAI chat completion")]
    private static partial void LogUnexpectedError(ILogger logger, Exception exception);
}
