using AI.DocumentIntelligence.Application.Abstractions.Ingestion;
using AI.DocumentIntelligence.Application.Abstractions.Persistence;
using AI.DocumentIntelligence.Application.Features.RAG.Ingest;
using AI.DocumentIntelligence.Domain.Entities;
using AI.DocumentIntelligence.Domain.Enums;
using MediatR;

namespace AI.DocumentIntelligence.Api.Infrastructure;

/// <summary>
/// Background consumer of <see cref="ChannelIngestionScheduler"/>. For each job it runs the
/// RAG ingest pipeline (chunk → embed → index) in a fresh DI scope and finalizes the
/// document's status: <c>Processed</c> on success, <c>Failed</c> (with reason) otherwise.
///
/// At startup it also fails any document left in <c>Processing</c> by a previous unclean
/// shutdown, so documents can never appear to be processing forever.
/// </summary>
internal sealed partial class IngestionWorker(
    ChannelIngestionScheduler scheduler,
    IServiceScopeFactory scopeFactory,
    ILogger<IngestionWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RecoverInterruptedDocumentsAsync(stoppingToken);

        await foreach (var job in scheduler.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await ProcessJobAsync(job, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Shutdown requested mid-job; startup recovery will fail the document.
                break;
            }
            catch (Exception ex)
            {
                // Never let one bad job kill the worker loop.
                LogJobCrashed(logger, job.DocumentId, ex);
                await TryMarkFailedAsync(job.DocumentId, "Unexpected ingestion error.", stoppingToken);
            }
        }
    }

    private async Task ProcessJobAsync(IngestionJob job, CancellationToken cancellationToken)
    {
        LogJobStarted(logger, job.DocumentId);

        using var scope = scopeFactory.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var ingestResult = await sender.Send(
            new IngestDocumentCommand(job.DocumentId, job.DocumentName, job.ExtractionResult),
            cancellationToken);

        var repository = unitOfWork.Repository<Document>();
        var document = await repository.GetByIdAsync(job.DocumentId, cancellationToken);
        if (document is null)
        {
            // Deleted while queued — nothing to finalize.
            LogDocumentGone(logger, job.DocumentId);
            return;
        }

        if (ingestResult.IsSuccess)
        {
            document.MarkProcessed(job.ExtractionResult.FullText);
            LogJobSucceeded(logger, job.DocumentId);
        }
        else
        {
            document.MarkFailed(ingestResult.Error.Description);
            LogJobFailed(logger, job.DocumentId, ingestResult.Error.Description);
        }

        repository.Update(document);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Fails documents stranded in <c>Processing</c> by a previous unclean shutdown.
    /// The in-process queue does not survive restarts, so these jobs will never run.
    /// </summary>
    private async Task RecoverInterruptedDocumentsAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var repository = unitOfWork.Repository<Document>();

            var stranded = await repository.FindAsync(
                d => d.Status == DocumentStatus.Processing, cancellationToken);

            if (stranded.Count == 0)
            {
                return;
            }

            foreach (var document in stranded)
            {
                document.MarkFailed(
                    "Ingestion was interrupted by an application restart. Please re-upload the document.");
                repository.Update(document);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);
            LogRecoveredStranded(logger, stranded.Count);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Recovery is best-effort (e.g., DB not reachable yet) — the worker must still start.
            LogRecoveryFailed(logger, ex);
        }
    }

    private async Task TryMarkFailedAsync(Guid documentId, string reason, CancellationToken cancellationToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var repository = unitOfWork.Repository<Document>();

            var document = await repository.GetByIdAsync(documentId, cancellationToken);
            if (document is null)
            {
                return;
            }

            document.MarkFailed(reason);
            repository.Update(document);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            LogFinalizeFailed(logger, documentId, ex);
        }
    }

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Ingestion started for document {DocumentId}")]
    private static partial void LogJobStarted(ILogger logger, Guid documentId);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Ingestion succeeded for document {DocumentId} — marked Processed")]
    private static partial void LogJobSucceeded(ILogger logger, Guid documentId);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Ingestion failed for document {DocumentId}: {Error} — marked Failed")]
    private static partial void LogJobFailed(ILogger logger, Guid documentId, string error);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Document {DocumentId} no longer exists — skipping status finalization")]
    private static partial void LogDocumentGone(ILogger logger, Guid documentId);

    [LoggerMessage(Level = LogLevel.Error,
        Message = "Unhandled exception while ingesting document {DocumentId}")]
    private static partial void LogJobCrashed(ILogger logger, Guid documentId, Exception exception);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Failed {Count} document(s) stranded in Processing by a previous shutdown")]
    private static partial void LogRecoveredStranded(ILogger logger, int count);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Startup recovery of stranded Processing documents failed — continuing")]
    private static partial void LogRecoveryFailed(ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Error,
        Message = "Could not mark document {DocumentId} as Failed after ingestion crash")]
    private static partial void LogFinalizeFailed(ILogger logger, Guid documentId, Exception exception);
}
