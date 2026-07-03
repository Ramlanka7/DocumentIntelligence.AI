using System.Net;
using System.Net.Http.Headers;
using System.Text;
using AI.DocumentIntelligence.Domain.ValueObjects;
using AI.DocumentIntelligence.Domain.Enums;
using AI.DocumentIntelligence.Domain.Entities;
using FluentAssertions;

namespace AI.DocumentIntelligence.Tests.Integration.Documents;

/// <summary>
/// Integration tests for document endpoints: authorization enforcement and upload validation.
/// Real file processing (extraction + ingestion) is skipped by using stub services.
/// </summary>
[Collection("Integration")]
public sealed class DocumentEndpointTests
{
    private readonly ApiWebApplicationFactory _factory;

    public DocumentEndpointTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // ---- GET /api/v1/documents (list) ----

    [Fact]
    public async Task ListDocuments_WithoutAuth_Returns401()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/v1/documents");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ListDocuments_AsViewer_Returns200()
    {
        var viewer = _factory.SeedViewerUser("viewer-list@test.com");
        var client = _factory.CreateAuthenticatedClient(viewer);

        var response = await client.GetAsync("/api/v1/documents");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ---- GET /api/v1/documents/{id} (get by id) ----

    [Fact]
    public async Task GetDocument_WithoutAuth_Returns401()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync($"/api/v1/documents/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetDocument_NotFound_Returns404()
    {
        var viewer = _factory.SeedViewerUser("viewer-get@test.com");
        var client = _factory.CreateAuthenticatedClient(viewer);

        var response = await client.GetAsync($"/api/v1/documents/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ---- POST /api/v1/documents (upload) ----

    [Fact]
    public async Task Upload_WithoutAuth_Returns401()
    {
        var client = _factory.CreateClient();

        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent("hello"u8.ToArray())
        {
            Headers = { ContentType = new MediaTypeHeaderValue("text/plain") }
        }, "file", "hello.txt");

        var response = await client.PostAsync("/api/v1/documents", content);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Upload_AsViewer_Returns403()
    {
        var viewer = _factory.SeedViewerUser("viewer-upload@test.com");
        var client = _factory.CreateAuthenticatedClient(viewer);

        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent("hello"u8.ToArray())
        {
            Headers = { ContentType = new MediaTypeHeaderValue("text/plain") }
        }, "file", "hello.txt");

        var response = await client.PostAsync("/api/v1/documents", content);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Upload_WithUnsupportedFileType_Returns400()
    {
        var analyst = _factory.SeedAnalystUser("analyst-upload@test.com");
        var client = _factory.CreateAuthenticatedClient(analyst);

        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent("<?xml version='1.0'?><root/>"u8.ToArray())
        {
            Headers = { ContentType = new MediaTypeHeaderValue("application/xml") }
        }, "file", "data.xml");

        var response = await client.PostAsync("/api/v1/documents", content);

        // Validation rejects the unsupported extension before even reaching the handler.
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Upload_TextFile_AsAnalyst_Returns201()
    {
        var analyst = _factory.SeedAnalystUser($"analyst-upload-txt-{Guid.NewGuid():N}@test.com");
        var client = _factory.CreateAuthenticatedClient(analyst);

        var textContent = Encoding.UTF8.GetBytes("This is a test plain-text document.");
        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(textContent)
        {
            Headers = { ContentType = new MediaTypeHeaderValue("text/plain") }
        }, "file", "sample.txt");

        var response = await client.PostAsync("/api/v1/documents", content);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    // ---- DELETE /api/v1/documents/{id} ----

    [Fact]
    public async Task DeleteDocument_WithoutAuth_Returns401()
    {
        var client = _factory.CreateClient();
        var response = await client.DeleteAsync($"/api/v1/documents/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteDocument_AsViewer_Returns403()
    {
        var viewer = _factory.SeedViewerUser("viewer-delete@test.com");
        var client = _factory.CreateAuthenticatedClient(viewer);

        var response = await client.DeleteAsync($"/api/v1/documents/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteDocument_NotFound_AsAnalyst_Returns404()
    {
        var analyst = _factory.SeedAnalystUser("analyst-delete@test.com");
        var client = _factory.CreateAuthenticatedClient(analyst);

        var response = await client.DeleteAsync($"/api/v1/documents/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ---- Health check ----

    [Fact]
    public async Task HealthLive_Always_Returns200()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/health/live");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
