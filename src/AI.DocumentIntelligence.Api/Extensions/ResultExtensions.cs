using AI.DocumentIntelligence.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace AI.DocumentIntelligence.Api.Extensions;

/// <summary>
/// Maps <see cref="Result"/> and <see cref="Result{TValue}"/> to appropriate
/// <see cref="IActionResult"/> HTTP responses using the RFC 7807 ProblemDetails convention.
/// </summary>
public static class ResultExtensions
{
    /// <summary>Maps a non-generic <see cref="Result"/> to an HTTP action result.</summary>
    public static IActionResult ToActionResult(this Result result, ControllerBase controller)
    {
        if (result.IsSuccess)
        {
            return controller.NoContent();
        }

        return result.Error.Type switch
        {
            ErrorType.Validation => controller.BadRequest(
                ProblemDetailsFor(result.Error, StatusCodes.Status400BadRequest)),
            ErrorType.NotFound => controller.NotFound(
                ProblemDetailsFor(result.Error, StatusCodes.Status404NotFound)),
            ErrorType.Conflict => controller.Conflict(
                ProblemDetailsFor(result.Error, StatusCodes.Status409Conflict)),
            ErrorType.Unauthorized => controller.Unauthorized(
                ProblemDetailsFor(result.Error, StatusCodes.Status401Unauthorized)),
            ErrorType.Forbidden => controller.StatusCode(StatusCodes.Status403Forbidden,
                ProblemDetailsFor(result.Error, StatusCodes.Status403Forbidden)),
            _ => controller.StatusCode(StatusCodes.Status500InternalServerError,
                ProblemDetailsFor(result.Error, StatusCodes.Status500InternalServerError)),
        };
    }

    /// <summary>Maps a <see cref="Result{TValue}"/> to an HTTP action result.</summary>
    public static IActionResult ToActionResult<TValue>(
        this Result<TValue> result,
        ControllerBase controller)
    {
        if (result.IsSuccess)
        {
            return controller.Ok(result.Value);
        }

        return ((Result)result).ToActionResult(controller);
    }

    private static ProblemDetails ProblemDetailsFor(Error error, int statusCode) =>
        new()
        {
            Status = statusCode,
            Title = error.Code,
            Detail = error.Description,
        };
}
