using System.Globalization;
using AI.DocumentIntelligence.Application.Abstractions.Documents;
using AI.DocumentIntelligence.Application.Contracts.Documents;
using AI.DocumentIntelligence.Domain.Common;
using CsvHelper;
using CsvHelper.Configuration;

namespace AI.DocumentIntelligence.Infrastructure.Documents.Processors;

/// <summary>
/// Extracts tabular data from CSV files using CsvHelper.
/// All data is treated as a single table on page 1; the first row is treated as a header.
/// </summary>
internal sealed class CsvDocumentProcessor : IDocumentProcessor
{
    public bool CanProcess(string fileName, string contentType)
        => Path.GetExtension(fileName).Equals(".csv", StringComparison.OrdinalIgnoreCase)
           || contentType.Equals("text/csv", StringComparison.OrdinalIgnoreCase)
           || contentType.Equals("application/csv", StringComparison.OrdinalIgnoreCase);

    public async Task<Result<DocumentExtractionResult>> ProcessAsync(
        Stream content,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await ExtractAsync(content, fileName, contentType, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Result.Failure<DocumentExtractionResult>(
                Error.Failure("Document.Csv.ProcessingFailed", $"Failed to process CSV: {ex.Message}"));
        }
    }

    private static async Task<Result<DocumentExtractionResult>> ExtractAsync(
        Stream content,
        string fileName,
        string contentType,
        CancellationToken cancellationToken)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            MissingFieldFound = null,
            BadDataFound = null,
        };

        using var reader = new StreamReader(content, leaveOpen: true);
        using var csv = new CsvReader(reader, config);

        var allRows = new List<IReadOnlyList<string>>();
        await csv.ReadAsync();
        csv.ReadHeader();
        var headers = csv.HeaderRecord;
        if (headers is { Length: > 0 })
        {
            allRows.Add(headers!);
        }

        while (await csv.ReadAsync())
        {
            cancellationToken.ThrowIfCancellationRequested();
            var row = new string[csv.ColumnCount];
            for (var col = 0; col < csv.ColumnCount; col++)
            {
                row[col] = csv.GetField(col) ?? string.Empty;
            }

            allRows.Add(row);
        }

        var fullTextLines = allRows.Select(r => string.Join(",", r));
        var fullText = string.Join("\n", fullTextLines);

        var page = new ExtractedPage(1, fullText);
        var table = new ExtractedTable(1, allRows);

        var metadata = new DocumentMetadata(
            fileName,
            contentType,
            content.CanSeek ? content.Length : 0L,
            1,
            null,
            null);

        return Result.Success(new DocumentExtractionResult(
            fullText,
            [page],
            [],
            [table],
            metadata));
    }
}
