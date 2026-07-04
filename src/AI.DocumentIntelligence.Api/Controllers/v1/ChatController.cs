using AI.DocumentIntelligence.Api.Extensions;
using AI.DocumentIntelligence.Application.Contracts.Chat;
using AI.DocumentIntelligence.Application.Features.Chat;
using AI.DocumentIntelligence.Application.Features.Chat.DeleteChatSession;
using AI.DocumentIntelligence.Application.Features.Chat.GetChatSession;
using AI.DocumentIntelligence.Application.Features.Chat.GetChatSessions;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AI.DocumentIntelligence.Api.Controllers.v1;

/// <summary>RAG-grounded chat: send a question about one or more documents and receive a cited answer.</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/chat")]
[Authorize(Policy = "ViewerOrAbove")]
[Produces("application/json")]
public sealed class ChatController(ISender sender) : ControllerBase
{
    /// <summary>Asks a question grounded in the specified documents and returns a cited AI answer.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ChatResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AskAsync(
        [FromBody] ChatCommand command,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return result.ToActionResult(this);
    }

    /// <summary>Returns a summary list of the current user's chat sessions.</summary>
    [HttpGet("sessions")]
    [Authorize(Policy = "AnalystOrAbove")]
    [ProducesResponseType(typeof(IReadOnlyList<ChatSessionSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetSessionsAsync(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetChatSessionsQuery(), cancellationToken);
        return result.ToActionResult(this);
    }

    /// <summary>Returns a single chat session with its ordered messages. Returns 404 if not owned.</summary>
    [HttpGet("sessions/{id:guid}")]
    [Authorize(Policy = "AnalystOrAbove")]
    [ProducesResponseType(typeof(ChatSessionDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSessionAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetChatSessionQuery(id), cancellationToken);
        return result.ToActionResult(this);
    }

    /// <summary>Deletes the current user's own chat session. Returns 404 if not owned.</summary>
    [HttpDelete("sessions/{id:guid}")]
    [Authorize(Policy = "AnalystOrAbove")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSessionAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DeleteChatSessionCommand(id), cancellationToken);
        return result.ToActionResult(this);
    }
}
