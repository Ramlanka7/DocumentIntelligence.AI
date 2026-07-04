using AI.DocumentIntelligence.Application.Contracts.Search;
using FluentAssertions;
using AppCitation = AI.DocumentIntelligence.Application.Contracts.Citation;
using DomainCitation = AI.DocumentIntelligence.Domain.ValueObjects.Citation;

namespace AI.DocumentIntelligence.Tests.RAG;

/// <summary>
/// Unit tests verifying the citation metadata mapping in the RAG pipeline:
/// - <see cref="DomainCitation.Create"/> domain validation
/// - Score → confidence clamping
/// - SearchHit → Citation projection used by the SearchDocumentsQueryHandler
/// </summary>
public sealed class CitationMappingTests
{
    // ---- Domain Citation.Create validation ----

    [Fact]
    public void CitationCreate_ValidInputs_ReturnsSuccess()
    {
        var result = DomainCitation.Create(
            documentId: Guid.NewGuid(),
            documentName: "contract.pdf",
            pageNumber: 3,
            paragraphReference: "¶2",
            snippet: "This clause defines...",
            confidenceScore: 0.85);

        result.IsSuccess.Should().BeTrue();
        result.Value.DocumentName.Should().Be("contract.pdf");
        result.Value.PageNumber.Should().Be(3);
        result.Value.ParagraphReference.Should().Be("¶2");
        result.Value.ConfidenceScore.Should().Be(0.85);
    }

    [Fact]
    public void CitationCreate_EmptyDocumentId_ReturnsFailure()
    {
        var result = DomainCitation.Create(
            documentId: Guid.Empty,
            documentName: "file.pdf",
            pageNumber: 1,
            paragraphReference: "¶1",
            snippet: "...",
            confidenceScore: 0.5);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Citation.InvalidDocumentId");
    }

    [Fact]
    public void CitationCreate_MissingDocumentName_ReturnsFailure()
    {
        var result = DomainCitation.Create(
            documentId: Guid.NewGuid(),
            documentName: string.Empty,
            pageNumber: 1,
            paragraphReference: "¶1",
            snippet: "...",
            confidenceScore: 0.5);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Citation.MissingDocumentName");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CitationCreate_InvalidPageNumber_ReturnsFailure(int pageNumber)
    {
        var result = DomainCitation.Create(
            documentId: Guid.NewGuid(),
            documentName: "doc.pdf",
            pageNumber: pageNumber,
            paragraphReference: "¶1",
            snippet: "...",
            confidenceScore: 0.5);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Citation.InvalidPageNumber");
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    [InlineData(2.0)]
    public void CitationCreate_InvalidConfidenceScore_ReturnsFailure(double confidence)
    {
        var result = DomainCitation.Create(
            documentId: Guid.NewGuid(),
            documentName: "doc.pdf",
            pageNumber: 1,
            paragraphReference: "¶1",
            snippet: "...",
            confidenceScore: confidence);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Citation.InvalidConfidenceScore");
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.5)]
    [InlineData(1.0)]
    public void CitationCreate_BoundaryConfidenceScores_Succeed(double confidence)
    {
        var result = DomainCitation.Create(
            documentId: Guid.NewGuid(),
            documentName: "doc.pdf",
            pageNumber: 1,
            paragraphReference: "¶1",
            snippet: "...",
            confidenceScore: confidence);

        result.IsSuccess.Should().BeTrue();
        result.Value.ConfidenceScore.Should().Be(confidence);
    }

    // ---- Score → confidence clamping (mirrors SearchDocumentsQueryHandler logic) ----

    [Theory]
    [InlineData(0.0, 0.0)]   // exactly 0
    [InlineData(0.75, 0.75)] // within [0,1]
    [InlineData(1.0, 1.0)]   // exactly 1
    [InlineData(1.5, 1.0)]   // above 1 → clamped
    [InlineData(-0.2, 0.0)]  // below 0 → clamped
    public void ScoreToConfidence_Clamps_ToUnitInterval(double rawScore, double expectedConfidence)
    {
        var confidence = Math.Min(1.0, Math.Max(0.0, rawScore));
        confidence.Should().Be(expectedConfidence);
    }

    // ---- SearchHit → Citation projection roundtrip ----

    [Fact]
    public void SearchHit_MapsToApplication_Citation_WithAllFields()
    {
        var docId = Guid.NewGuid();
        var hit = new SearchHit(
            DocumentId: docId,
            DocumentName: "proposal.docx",
            PageNumber: 5,
            ParagraphReference: "Section 3.1",
            Content: "The vendor agrees to deliver...",
            Score: 0.91);

        var confidence = Math.Min(1.0, Math.Max(0.0, hit.Score));
        var domainCitationResult = DomainCitation.Create(
            documentId: hit.DocumentId,
            documentName: hit.DocumentName,
            pageNumber: Math.Max(1, hit.PageNumber),
            paragraphReference: hit.ParagraphReference,
            snippet: hit.Content,
            confidenceScore: confidence);

        domainCitationResult.IsSuccess.Should().BeTrue();
        var domainCitation = domainCitationResult.Value;

        var appCitation = new AppCitation(
            DocumentId: domainCitation.DocumentId,
            DocumentName: domainCitation.DocumentName,
            PageNumber: domainCitation.PageNumber,
            ParagraphReference: domainCitation.ParagraphReference,
            Snippet: domainCitation.Snippet,
            ConfidenceScore: domainCitation.ConfidenceScore);

        appCitation.DocumentId.Should().Be(docId);
        appCitation.DocumentName.Should().Be("proposal.docx");
        appCitation.PageNumber.Should().Be(5);
        appCitation.ParagraphReference.Should().Be("Section 3.1");
        appCitation.Snippet.Should().Be("The vendor agrees to deliver...");
        appCitation.ConfidenceScore.Should().BeApproximately(0.91, 0.0001);
    }

    [Fact]
    public void SearchHit_WithPageNumberZero_ClampedToOne()
    {
        var hit = new SearchHit(
            DocumentId: Guid.NewGuid(),
            DocumentName: "doc.pdf",
            PageNumber: 0,  // invalid from index — should be clamped
            ParagraphReference: "¶1",
            Content: "Some content",
            Score: 0.5);

        var pageNumber = Math.Max(1, hit.PageNumber);
        pageNumber.Should().Be(1);

        var domainCitationResult = DomainCitation.Create(
            documentId: hit.DocumentId,
            documentName: hit.DocumentName,
            pageNumber: pageNumber,
            paragraphReference: hit.ParagraphReference,
            snippet: hit.Content,
            confidenceScore: Math.Min(1.0, Math.Max(0.0, hit.Score)));

        domainCitationResult.IsSuccess.Should().BeTrue();
    }

    // ---- Application SearchableChunk integrity ----

    [Fact]
    public void SearchableChunk_PreservesAllCitationFields()
    {
        var docId = Guid.NewGuid();
        var embedding = Enumerable.Range(0, 1536).Select(i => (float)i / 1536).ToArray();

        var chunk = new SearchableChunk(
            DocumentId: docId,
            DocumentName: "report.pdf",
            ChunkIndex: 2,
            Content: "Risk section content.",
            PageNumber: 8,
            ParagraphReference: "Risk Assessment",
            Embedding: embedding);

        chunk.DocumentId.Should().Be(docId);
        chunk.DocumentName.Should().Be("report.pdf");
        chunk.ChunkIndex.Should().Be(2);
        chunk.PageNumber.Should().Be(8);
        chunk.ParagraphReference.Should().Be("Risk Assessment");
        chunk.Embedding.Count.Should().Be(1536);
    }
}
