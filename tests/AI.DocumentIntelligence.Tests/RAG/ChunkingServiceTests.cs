using AI.DocumentIntelligence.Application.Contracts.Documents;
using AI.DocumentIntelligence.Infrastructure.Documents.Chunking;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AI.DocumentIntelligence.Tests.RAG;

/// <summary>
/// Unit tests for <see cref="ChunkingService"/>: sliding-window logic, section-aware chunking,
/// citation metadata (page number + paragraph reference) preservation, and configurable parameters.
/// </summary>
public sealed class ChunkingServiceTests
{
    private readonly Guid _documentId = Guid.NewGuid();

    private static ChunkingService CreateSut(Action<ChunkingOptions>? configure = null)
    {
        var options = new ChunkingOptions();
        configure?.Invoke(options);
        return new ChunkingService(
            Options.Create(options),
            NullLogger<ChunkingService>.Instance);
    }

    private static DocumentExtractionResult MakeResult(
        string fullText = "",
        IReadOnlyList<ExtractedPage>? pages = null,
        IReadOnlyList<ExtractedSection>? sections = null)
        => new(
            FullText: fullText,
            Pages: pages ?? [],
            Sections: sections ?? [],
            Tables: [],
            Metadata: new DocumentMetadata("test.txt", "text/plain", 100, 1, null, null));

    // ---- Sliding-window tests ----

    [Fact]
    public async Task ChunkAsync_EmptyContent_ReturnsZeroChunks()
    {
        var sut = CreateSut();
        var result = MakeResult(fullText: string.Empty);

        var outcome = await sut.ChunkAsync(_documentId, result);

        outcome.IsSuccess.Should().BeTrue();
        outcome.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task ChunkAsync_ContentShorterThanChunkSize_ProducesSingleChunk()
    {
        var sut = CreateSut(o =>
        {
            o.ChunkSize = 500;
            o.ChunkOverlap = 50;
            o.SectionAware = false;
            o.MinChunkSize = 1;
        });

        var text = new string('A', 100);
        var pages = new[] { new ExtractedPage(1, text) };
        var result = MakeResult(fullText: text, pages: pages);

        var outcome = await sut.ChunkAsync(_documentId, result);

        outcome.IsSuccess.Should().BeTrue();
        outcome.Value.Should().HaveCount(1);
        outcome.Value[0].Content.Should().Be(text);
    }

    [Fact]
    public async Task ChunkAsync_ContentLongerThanChunkSize_ProducesMultipleChunks()
    {
        var sut = CreateSut(o =>
        {
            o.ChunkSize = 100;
            o.ChunkOverlap = 20;
            o.SectionAware = false;
            o.MinChunkSize = 1;
        });

        var text = new string('X', 350);
        var pages = new[] { new ExtractedPage(1, text) };
        var result = MakeResult(fullText: text, pages: pages);

        var outcome = await sut.ChunkAsync(_documentId, result);

        outcome.IsSuccess.Should().BeTrue();
        outcome.Value.Count.Should().BeGreaterThan(1);
    }

    [Fact]
    public async Task ChunkAsync_SlidingWindow_ProducesOverlappingChunks()
    {
        const int chunkSize = 10;
        const int overlap = 4;

        var sut = CreateSut(o =>
        {
            o.ChunkSize = chunkSize;
            o.ChunkOverlap = overlap;
            o.SectionAware = false;
            o.MinChunkSize = 1;
        });

        // "ABCDEFGHIJKLMNOPQRSTUVWXYZ" — 26 chars
        var text = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        var pages = new[] { new ExtractedPage(1, text) };
        var result = MakeResult(fullText: text, pages: pages);

        var outcome = await sut.ChunkAsync(_documentId, result);

        outcome.IsSuccess.Should().BeTrue();
        var chunks = outcome.Value.ToList();

        // First chunk: 0..9 = "ABCDEFGHIJ"
        chunks[0].Content.Should().StartWith("ABCDEFGHIJ");

        // Second chunk starts at position (chunkSize - overlap) = 6: "GHIJKLMNOP"
        chunks[1].Content.Should().StartWith("GHIJKLMNOP");

        // Overlap: chunks 0 and 1 share chars at positions 6-9 ("GHIJ")
        chunks[0].Content[^4..].Should().Be(chunks[1].Content[..4]);
    }

    // ---- Citation metadata preservation tests ----

    [Fact]
    public async Task ChunkAsync_PageBased_PreservesPageNumber()
    {
        var sut = CreateSut(o =>
        {
            o.ChunkSize = 50;
            o.ChunkOverlap = 0;
            o.SectionAware = false;
            o.MinChunkSize = 1;
        });

        var pages = new[]
        {
            new ExtractedPage(1, "Page one content here."),
            new ExtractedPage(2, "Page two content here."),
        };

        var result = MakeResult(fullText: "Page one content here.\nPage two content here.", pages: pages);

        var outcome = await sut.ChunkAsync(_documentId, result);

        outcome.IsSuccess.Should().BeTrue();
        outcome.Value.Should().HaveCountGreaterOrEqualTo(2);

        outcome.Value.Any(c => c.PageNumber == 1).Should().BeTrue();
        outcome.Value.Any(c => c.PageNumber == 2).Should().BeTrue();
    }

    [Fact]
    public async Task ChunkAsync_PageBased_ParagraphReferenceContainsPageAndIndex()
    {
        var sut = CreateSut(o =>
        {
            o.ChunkSize = 200;
            o.ChunkOverlap = 0;
            o.SectionAware = false;
            o.MinChunkSize = 1;
        });

        var pages = new[] { new ExtractedPage(3, "Content on page three.") };
        var result = MakeResult(fullText: "Content on page three.", pages: pages);

        var outcome = await sut.ChunkAsync(_documentId, result);

        outcome.IsSuccess.Should().BeTrue();
        outcome.Value[0].ParagraphReference.Should().Contain("p3");
    }

    [Fact]
    public async Task ChunkAsync_SectionAware_ChunksAreBoundedBySectionHeading()
    {
        var sut = CreateSut(o =>
        {
            o.ChunkSize = 500;
            o.ChunkOverlap = 0;
            o.SectionAware = true;
            o.MinChunkSize = 1;
        });

        var sections = new[]
        {
            new ExtractedSection("Introduction", 1, 1, "This is the introduction content."),
            new ExtractedSection("Conclusion", 1, 2, "This is the conclusion content."),
        };

        var result = MakeResult(sections: sections);

        var outcome = await sut.ChunkAsync(_documentId, result);

        outcome.IsSuccess.Should().BeTrue();
        outcome.Value.Should().HaveCount(2);

        // Each chunk's paragraph reference should be the section heading.
        outcome.Value[0].ParagraphReference.Should().Be("Introduction");
        outcome.Value[1].ParagraphReference.Should().Be("Conclusion");
    }

    [Fact]
    public async Task ChunkAsync_SectionAware_PreservesStartPage()
    {
        var sut = CreateSut(o =>
        {
            o.ChunkSize = 500;
            o.ChunkOverlap = 0;
            o.SectionAware = true;
            o.MinChunkSize = 1;
        });

        var sections = new[]
        {
            new ExtractedSection("Section A", 1, 4, "Content on page four."),
            new ExtractedSection("Section B", 1, 7, "Content on page seven."),
        };

        var result = MakeResult(sections: sections);

        var outcome = await sut.ChunkAsync(_documentId, result);

        outcome.IsSuccess.Should().BeTrue();
        outcome.Value[0].PageNumber.Should().Be(4);
        outcome.Value[1].PageNumber.Should().Be(7);
    }

    // ---- Domain entity integrity tests ----

    [Fact]
    public async Task ChunkAsync_AssignsSequentialChunkIndices()
    {
        var sut = CreateSut(o =>
        {
            o.ChunkSize = 20;
            o.ChunkOverlap = 0;
            o.SectionAware = false;
            o.MinChunkSize = 1;
        });

        var text = new string('Z', 100);
        var pages = new[] { new ExtractedPage(1, text) };
        var result = MakeResult(fullText: text, pages: pages);

        var outcome = await sut.ChunkAsync(_documentId, result);

        outcome.IsSuccess.Should().BeTrue();
        var indices = outcome.Value.Select(c => c.Index).ToList();
        indices.Should().BeInAscendingOrder();
        indices[0].Should().Be(0);
    }

    [Fact]
    public async Task ChunkAsync_AllChunksHaveDocumentId()
    {
        var sut = CreateSut(o =>
        {
            o.ChunkSize = 50;
            o.ChunkOverlap = 0;
            o.SectionAware = false;
            o.MinChunkSize = 1;
        });

        var text = new string('Q', 200);
        var pages = new[] { new ExtractedPage(1, text) };
        var result = MakeResult(fullText: text, pages: pages);

        var outcome = await sut.ChunkAsync(_documentId, result);

        outcome.IsSuccess.Should().BeTrue();
        outcome.Value.Should().AllSatisfy(c => c.DocumentId.Should().Be(_documentId));
    }

    [Fact]
    public async Task ChunkAsync_MinChunkSize_FiltersOutShortSlices()
    {
        var sut = CreateSut(o =>
        {
            o.ChunkSize = 50;
            o.ChunkOverlap = 49; // step = 1, so last slice may be 1 char
            o.SectionAware = false;
            o.MinChunkSize = 10;
        });

        var text = new string('A', 55);
        var pages = new[] { new ExtractedPage(1, text) };
        var result = MakeResult(fullText: text, pages: pages);

        var outcome = await sut.ChunkAsync(_documentId, result);

        outcome.IsSuccess.Should().BeTrue();
        outcome.Value.Should().AllSatisfy(c => c.Content.Length.Should().BeGreaterOrEqualTo(10));
    }

    // ---- Configurable parameters test ----

    [Fact]
    public async Task ChunkAsync_ChunkSizeIsConfigurable()
    {
        var text = new string('B', 500);
        var pages = new[] { new ExtractedPage(1, text) };
        var resultData = MakeResult(fullText: text, pages: pages);

        var sutSmall = CreateSut(o =>
        {
            o.ChunkSize = 50;
            o.ChunkOverlap = 0;
            o.SectionAware = false;
            o.MinChunkSize = 1;
        });

        var sutLarge = CreateSut(o =>
        {
            o.ChunkSize = 250;
            o.ChunkOverlap = 0;
            o.SectionAware = false;
            o.MinChunkSize = 1;
        });

        var smallOutcome = await sutSmall.ChunkAsync(_documentId, resultData);
        var largeOutcome = await sutLarge.ChunkAsync(_documentId, resultData);

        smallOutcome.IsSuccess.Should().BeTrue();
        largeOutcome.IsSuccess.Should().BeTrue();
        smallOutcome.Value.Count.Should().BeGreaterThan(largeOutcome.Value.Count);
    }
}
