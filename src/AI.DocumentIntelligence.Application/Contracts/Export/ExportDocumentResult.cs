namespace AI.DocumentIntelligence.Application.Contracts.Export;

/// <summary>
/// The binary result of an export operation: the generated file bytes, its MIME content type, and
/// the suggested download filename. Consumed directly by the export controller to produce a file response.
/// </summary>
/// <param name="Content">The generated file bytes.</param>
/// <param name="ContentType">The MIME content type (e.g. "application/pdf").</param>
/// <param name="FileName">The suggested download filename including extension.</param>
public sealed record ExportDocumentResult(
    byte[] Content,
    string ContentType,
    string FileName);
