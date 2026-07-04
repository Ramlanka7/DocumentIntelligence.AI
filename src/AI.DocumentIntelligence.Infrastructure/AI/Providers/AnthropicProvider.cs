using System.Diagnostics;
using Anthropic.SDK;
using Anthropic.SDK.Messaging;
using AI.DocumentIntelligence.Application.Abstractions.AI;
using AI.DocumentIntelligence.Application.Contracts.AI;
using AI.DocumentIntelligence.Domain.Common;
using AI.DocumentIntelligence.Infrastructure.AI.Options;
using AI.DocumentIntelligence.Infrastructure.Telemetry;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using DomainError = AI.DocumentIntelligence.Domain.Common.Error;

namespace AI.DocumentIntelligence.Infrastructure.AI.Providers;

/// <summary>
/// <see cref="IAIProvider"/> implementation backed by Anthropic Claude. Selectable via
/// <c>AI:ProviderName = "Anthropic"</c> in configuration.
/// System messages in <see cref="AiCompletionRequest"/> are extracted and forwarded as
/// Anthropic's top-level <c>system</c> parameter, which is separate from the conversation.
/// </summary>
internal sealed partial class AnthropicProvider : IAIProvider, IDisposable
{
    public const string ProviderName = "Anthropic";

    private readonly AnthropicClient _client;
    private readonly AnthropicOptions _options;
    private readonly ILogger<AnthropicProvider> _logger;

    public string Name => ProviderName;

    public AnthropicProvider(
        IOptions<AnthropicOptions> options,
        ILogger<AnthropicProvider> logger)
    {
        _options = options.Value;
        _logger = logger;
        _client = new AnthropicClient(new APIAuthentication(_options.ApiKey));
    }

    public async Task<Result<AiCompletionResult>> CompleteAsync(
        AiCompletionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            return Result.Failure<AiCompletionResult>(
                DomainError.Failure("Anthropic.NotConfigured",
                    "Anthropic is not configured. Set Anthropic:ApiKey to a valid API key (starts with 'sk-ant-')."));
        }

        using var activity = InfrastructureActivitySource.Source.StartActivity(
            "ai.completion", ActivityKind.Client);
        activity?.SetTag("ai.provider", ProviderName);
        activity?.SetTag("ai.model", _options.Model);
        activity?.SetTag("ai.message_count", request.Messages.Count);

        var sw = Stopwatch.StartNew();

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            sw.Stop();
            activity?.SetStatus(ActivityStatusCode.Error, "Anthropic is not configured (missing ApiKey).");
            RecordCompletionMetrics(sw.Elapsed.TotalMilliseconds, 0, 0, 0m, "not_configured");
            return Result.Failure<AiCompletionResult>(
                DomainError.Failure("Anthropic.NotConfigured", "Anthropic is not configured (missing ApiKey)."));
        }

        try
        {
            LogCompletionRequest(_logger, _options.Model, request.Messages.Count);
            // Anthropic treats system prompts separately from the conversation messages.
            var systemMessages = request.Messages
                .Where(m => m.Role == AiRole.System)
                .Select(m => new SystemMessage(m.Content))
                .ToList();

            var conversationMessages = request.Messages
                .Where(m => m.Role != AiRole.System)
                .Select(m => new Message(
                    m.Role == AiRole.Assistant ? RoleType.Assistant : RoleType.User,
                    m.Content))
                .ToList();

            var parameters = new MessageParameters
            {
                Model = _options.Model,
                MaxTokens = request.MaxTokens ?? _options.MaxTokens,
                Temperature = (decimal)request.Temperature,
                System = systemMessages.Count > 0 ? systemMessages : null,
                Messages = conversationMessages
            };

            var response = await _client.Messages.GetClaudeMessageAsync(parameters, cancellationToken);

            var text = response.Content.OfType<TextContent>().FirstOrDefault()?.Text ?? string.Empty;

            if (string.IsNullOrEmpty(text))
            {
                sw.Stop();
                RecordCompletionMetrics(sw.Elapsed.TotalMilliseconds, 0, 0, 0m, "empty_response");
                activity?.SetStatus(ActivityStatusCode.Error, "Anthropic returned an empty response.");
                return Result.Failure<AiCompletionResult>(
                    DomainError.Failure("Anthropic.EmptyResponse", "Anthropic returned an empty completion response."));
            }

            var promptTokens = response.Usage?.InputTokens ?? 0;
            var completionTokens = response.Usage?.OutputTokens ?? 0;
            var estimatedCost = CalculateCost(promptTokens, completionTokens);
            var modelUsed = response.Model ?? _options.Model;

            sw.Stop();

            activity?.SetTag("ai.tokens.prompt", promptTokens);
            activity?.SetTag("ai.tokens.completion", completionTokens);
            activity?.SetStatus(ActivityStatusCode.Ok);

            RecordCompletionMetrics(sw.Elapsed.TotalMilliseconds, promptTokens, completionTokens, estimatedCost, "success");
            LogCompletionReceived(_logger, promptTokens, completionTokens, modelUsed);

            return Result.Success(new AiCompletionResult(
                text,
                new TokenUsage(promptTokens, completionTokens, estimatedCost),
                modelUsed));
        }
        catch (Exception ex)
        {
            sw.Stop();
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            RecordCompletionMetrics(sw.Elapsed.TotalMilliseconds, 0, 0, 0m, "unexpected_error");
            LogUnexpectedError(_logger, ex);
            return Result.Failure<AiCompletionResult>(
                DomainError.Failure("Anthropic.UnexpectedError",
                    $"Unexpected error during Anthropic chat completion: {ex.Message}"));
        }
    }

    public void Dispose() => _client.Dispose();

    private void RecordCompletionMetrics(
        double durationMs,
        int promptTokens,
        int completionTokens,
        decimal cost,
        string status)
    {
        var tags = new TagList
        {
            { "ai.provider", ProviderName },
            { "ai.model", _options.Model },
            { "status", status },
        };

        InfrastructureActivitySource.AiCompletionRequests.Add(1, tags);
        InfrastructureActivitySource.AiCompletionDurationMs.Record(durationMs, tags);

        if (promptTokens + completionTokens > 0)
        {
            InfrastructureActivitySource.AiTokensConsumed.Add(promptTokens + completionTokens, tags);
            InfrastructureActivitySource.AiCompletionCostUsd.Record((double)cost, tags);
        }
    }

    private decimal CalculateCost(int promptTokens, int completionTokens) =>
        (promptTokens * _options.InputCostPerMillionTokens
         + completionTokens * _options.OutputCostPerMillionTokens)
        / 1_000_000m;

    [LoggerMessage(Level = LogLevel.Debug,
        Message = "Requesting Anthropic completion via model '{Model}' with {MessageCount} message(s)")]
    private static partial void LogCompletionRequest(ILogger logger, string model, int messageCount);

    [LoggerMessage(Level = LogLevel.Debug,
        Message = "Anthropic completion received — {PromptTokens} input, {CompletionTokens} output tokens via '{Model}'")]
    private static partial void LogCompletionReceived(ILogger logger, int promptTokens, int completionTokens, string model);

    [LoggerMessage(Level = LogLevel.Error,
        Message = "Unexpected error during Anthropic chat completion")]
    private static partial void LogUnexpectedError(ILogger logger, Exception exception);
}
