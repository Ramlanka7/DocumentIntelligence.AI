using AI.DocumentIntelligence.Api.Extensions;
using AI.DocumentIntelligence.Application.Features.Documents.Delete;
using AI.DocumentIntelligence.Application.Features.Documents.List;
using AI.DocumentIntelligence.Application.Features.Documents.Queries;
using AI.DocumentIntelligence.Application.Features.Documents.Upload;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AI.DocumentIntelligence.Api.Controllers.v1;

/// <summary>Manages document upload, retrieval, and deletion.</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/documents")]
[Authorize(Policy = "ViewerOrAbove")]
[Produces("application/json")]
public sealed class DocumentsController(ISender sender) : ControllerBase
{
    private const long MaxUploadBytes = 100 * 1024 * 1024; // 100 MB

    /// <summary>Uploads a document, extracts its text, and queues it for RAG ingestion.</summary>
    [HttpPost]
    [Authorize(Policy = "AnalystOrAbove")]
    [RequestSizeLimit(MaxUploadBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxUploadBytes)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(UploadDocumentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UploadAsync(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        var command = new UploadDocumentCommand(
            file.FileName,
            file.ContentType,
            file.Length,
            file.OpenReadStream());

        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.ToActionResult(this);
        }

        return CreatedAtAction(nameof(GetAsync), new { id = result.Value.DocumentId, version = RouteData.Values["version"] }, result.Value);
    }

    /// <summary>Returns a summary list of all documents belonging to the current user.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<DocumentSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ListAsync(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ListDocumentsQuery(), cancellationToken);
        return result.ToActionResult(this);
    }

    /// <summary>Returns the detail view of a single document.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(DocumentDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetDocumentQuery(id), cancellationToken);
        return result.ToActionResult(this);
    }

    /// <summary>Deletes a document and its stored file. Owner or Admin only.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "AnalystOrAbove")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DeleteDocumentCommand(id), cancellationToken);
        return result.ToActionResult(this);
    }
}
