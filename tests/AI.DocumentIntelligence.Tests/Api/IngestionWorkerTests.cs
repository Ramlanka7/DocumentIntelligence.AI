using AI.DocumentIntelligence.Api.Infrastructure;
using AI.DocumentIntelligence.Application.Abstractions.Ingestion;
using AI.DocumentIntelligence.Application.Abstractions.Persistence;
using AI.DocumentIntelligence.Application.Contracts.Documents;
using AI.DocumentIntelligence.Application.Features.RAG.Ingest;
using AI.DocumentIntelligence.Domain.Common;
using AI.DocumentIntelligence.Domain.Entities;
using AI.DocumentIntelligence.Domain.Enums;
using AI.DocumentIntelligence.Domain.ValueObjects;
using AI.DocumentIntelligence.Tests.Integration.Fakes;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace AI.DocumentIntelligence.Tests.Api;

/// <summary>
/// Behavioural tests for the background ingestion pipeline: a scheduled job must move the
/// document from <c>Processing</c> to <c>Processed</c> (success) or <c>Failed</c> (with the
/// ingest error as the reason), and documents stranded in <c>Processing</c> by a previous
/// shutdown must be failed at startup.
/// </summary>
public sealed class IngestionWorkerTests
{
    private readonly InMemoryUnitOfWork _uow = new();
    private readonly Mock<ISender> _senderMock = new();

    private (IngestionWorker Worker, ChannelIngestionScheduler Scheduler) CreateWorker()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IUnitOfWork>(_uow);
        services.AddSingleton(_senderMock.Object);
        var provider = services.BuildServiceProvider();

        var scheduler = new ChannelIngestionScheduler();
        var worker = new IngestionWorker(
            scheduler,
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<IngestionWorker>.Instance);

        return (worker, scheduler);
    }

    private Document SeedProcessingDocument()
    {
        var document = Document.Create(
            Guid.NewGuid(),
            new FileMetadata("doc.pdf", 100, 1, "application/pdf"),
            DocumentType.Pdf,
            "storage/doc.pdf");
        document.MarkProcessing();

        ((IUnitOfWork)_uow).Repository<Document>()
            .AddAsync(document).GetAwaiter().GetResult();
        return document;
    }

    private static IngestionJob JobFor(Document document) =>
        new(
            document.Id,
            "doc.pdf",
            new DocumentExtractionResult(
                "full text",
                [new ExtractedPage(1, "full text")],
                [],
                [],
                new DocumentMetadata("doc.pdf", "application/pdf", 100, 1, null, null)));

    /// <summary>Polls until the document leaves Processing or the timeout elapses.</summary>
    private async Task<Document> WaitForFinalStatusAsync(Guid documentId)
    {
        var repo = _uow.GetRepository<Document>();
        for (var i = 0; i < 100; i++)
        {
            var document = repo.All.Single(d => d.Id == documentId);
            if (document.Status != DocumentStatus.Processing)
            {
                return document;
            }

            await Task.Delay(50);
        }

        return repo.All.Single(d => d.Id == documentId);
    }

    [Fact]
    public async Task ScheduledJob_OnIngestSuccess_MarksDocumentProcessed()
    {
        _senderMock
            .Setup(s => s.Send(It.IsAny<IngestDocumentCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var (worker, scheduler) = CreateWorker();
        await worker.StartAsync(CancellationToken.None);
        try
        {
            var document = SeedProcessingDocument();

            await scheduler.ScheduleAsync(JobFor(document));

            var finalDocument = await WaitForFinalStatusAsync(document.Id);
            finalDocument.Status.Should().Be(DocumentStatus.Processed);
            finalDocument.ExtractedText.Should().Be("full text");
        }
        finally
        {
            await worker.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task ScheduledJob_OnIngestFailure_MarksDocumentFailedWithReason()
    {
        _senderMock
            .Setup(s => s.Send(It.IsAny<IngestDocumentCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(Error.Failure("Embed.Down", "embedding service unavailable")));

        var (worker, scheduler) = CreateWorker();
        await worker.StartAsync(CancellationToken.None);
        try
        {
            var document = SeedProcessingDocument();

            await scheduler.ScheduleAsync(JobFor(document));

            var finalDocument = await WaitForFinalStatusAsync(document.Id);
            finalDocument.Status.Should().Be(DocumentStatus.Failed);
            finalDocument.FailureReason.Should().Be("embedding service unavailable");
        }
        finally
        {
            await worker.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task Startup_FailsDocumentsStrandedInProcessing()
    {
        // A document left Processing by a previous unclean shutdown, with no queued job.
        var stranded = SeedProcessingDocument();

        var (worker, _) = CreateWorker();
        await worker.StartAsync(CancellationToken.None);
        try
        {
            var finalDocument = await WaitForFinalStatusAsync(stranded.Id);
            finalDocument.Status.Should().Be(DocumentStatus.Failed);
            finalDocument.FailureReason.Should().Contain("restart");
        }
        finally
        {
            await worker.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task ScheduledJob_WhenDocumentWasDeleted_CompletesWithoutError()
    {
        _senderMock
            .Setup(s => s.Send(It.IsAny<IngestDocumentCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var (worker, scheduler) = CreateWorker();
        await worker.StartAsync(CancellationToken.None);
        try
        {
            // Job references a document that no longer exists (deleted while queued).
            var ghost = Document.Create(
                Guid.NewGuid(),
                new FileMetadata("gone.pdf", 1, 1, "application/pdf"),
                DocumentType.Pdf,
                "storage/gone.pdf");

            await scheduler.ScheduleAsync(JobFor(ghost));

            // Schedule a second, real job afterwards to prove the worker loop survived.
            var document = SeedProcessingDocument();
            await scheduler.ScheduleAsync(JobFor(document));

            var finalDocument = await WaitForFinalStatusAsync(document.Id);
            finalDocument.Status.Should().Be(DocumentStatus.Processed);
        }
        finally
        {
            await worker.StopAsync(CancellationToken.None);
        }
    }
}
