using System.Text;
using AI.DocumentIntelligence.Infrastructure.Storage;
using Azure.Storage.Blobs;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Testcontainers.Azurite;

namespace AI.DocumentIntelligence.Tests.Integration.Storage;

/// <summary>
/// Integration tests for <see cref="AzureBlobFileStorage"/> against an Azurite Testcontainer
/// (<c>mcr.microsoft.com/azure-storage/azurite</c>).
///
/// Exercises a full save → get → delete round-trip to verify that the Azure Blob Storage
/// implementation behaves correctly without requiring a real Azure subscription.
///
/// Container: Azurite 3.x — same image as docker-compose.yml.
/// </summary>
[Collection("Docker")]
public sealed class AzureBlobFileStorageTests : IAsyncLifetime
{
    // ---- Testcontainer setup ----

    private readonly AzuriteContainer _azurite = new AzuriteBuilder()
        .WithImage("mcr.microsoft.com/azure-storage/azurite:3.33.0")
        .Build();

    // Resolved after the container starts.
    private AzureBlobFileStorage _storage = null!;

    // ---- IAsyncLifetime ----

    public async Task InitializeAsync()
    {
        await _azurite.StartAsync();

        var connectionString = _azurite.GetConnectionString();

        var options = Options.Create(new BlobStorageOptions
        {
            ConnectionString = connectionString,
            ContainerName = "test-blobs",
        });

        var serviceClient = new BlobServiceClient(connectionString);
        _storage = new AzureBlobFileStorage(
            serviceClient,
            options,
            NullLogger<AzureBlobFileStorage>.Instance);
    }

    public async Task DisposeAsync() => await _azurite.DisposeAsync();

    // ---- Tests ----

    [Fact]
    public async Task SaveAsync_ValidStream_ReturnsSuccessWithKey()
    {
        // Arrange
        using var content = new MemoryStream(Encoding.UTF8.GetBytes("Hello, Blob Storage!"));

        // Act
        var result = await _storage.SaveAsync(content, "hello.txt", "text/plain");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNullOrWhiteSpace();
        result.Value.Should().EndWith("/hello.txt",
            because: "the key is formatted as {guid:N}/{sanitizedFileName}");
    }

    [Fact]
    public async Task SaveAsync_CreatesContainerIfNotExists_Idempotent()
    {
        // Calling SaveAsync twice creates the container once and succeeds both times.
        using var content1 = new MemoryStream(Encoding.UTF8.GetBytes("First file"));
        using var content2 = new MemoryStream(Encoding.UTF8.GetBytes("Second file"));

        var result1 = await _storage.SaveAsync(content1, "file1.txt", "text/plain");
        var result2 = await _storage.SaveAsync(content2, "file2.txt", "text/plain");

        result1.IsSuccess.Should().BeTrue();
        result2.IsSuccess.Should().BeTrue();
        result1.Value.Should().NotBe(result2.Value, because: "each upload gets a unique key");
    }

    [Fact]
    public async Task GetAsync_ExistingBlob_ReturnsStreamWithCorrectContent()
    {
        // Arrange
        const string originalContent = "The quick brown fox";
        using var uploadStream = new MemoryStream(Encoding.UTF8.GetBytes(originalContent));

        var saveResult = await _storage.SaveAsync(uploadStream, "fox.txt", "text/plain");
        saveResult.IsSuccess.Should().BeTrue("prerequisite: upload must succeed");

        // Act
        var getResult = await _storage.GetAsync(saveResult.Value);

        // Assert
        getResult.IsSuccess.Should().BeTrue();
        await using var downloadedStream = getResult.Value;
        using var reader = new StreamReader(downloadedStream, Encoding.UTF8);
        var downloadedContent = await reader.ReadToEndAsync();
        downloadedContent.Should().Be(originalContent);
    }

    [Fact]
    public async Task GetAsync_NonExistentBlob_ReturnsFailure()
    {
        // Act
        var result = await _storage.GetAsync("nonexistent-guid/missing.txt");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Document.NotFound",
            because: "a missing blob maps to DomainErrors.Document.NotFound");
    }

    [Fact]
    public async Task DeleteAsync_ExistingBlob_ReturnsSuccess_AndBlobIsGone()
    {
        // Arrange
        using var content = new MemoryStream(Encoding.UTF8.GetBytes("to be deleted"));
        var saveResult = await _storage.SaveAsync(content, "delete-me.txt", "text/plain");
        saveResult.IsSuccess.Should().BeTrue("prerequisite: upload must succeed");
        var key = saveResult.Value;

        // Act
        var deleteResult = await _storage.DeleteAsync(key);

        // Assert
        deleteResult.IsSuccess.Should().BeTrue();

        // Confirm the blob is truly gone.
        var getResult = await _storage.GetAsync(key);
        getResult.IsFailure.Should().BeTrue("the blob should no longer exist after deletion");
    }

    [Fact]
    public async Task DeleteAsync_NonExistentBlob_ReturnsSuccess()
    {
        // DeleteIfExists should succeed even when the blob does not exist.
        var result = await _storage.DeleteAsync("nonexistent-guid/ghost.txt");

        result.IsSuccess.Should().BeTrue(
            because: "DeleteIfExists is idempotent — deleting a non-existent blob is not an error");
    }

    [Fact]
    public async Task SaveAsync_SetsContentTypeOnBlob()
    {
        // Arrange
        using var content = new MemoryStream(Encoding.UTF8.GetBytes("{}"));
        const string expectedContentType = "application/json";

        var saveResult = await _storage.SaveAsync(content, "data.json", expectedContentType);
        saveResult.IsSuccess.Should().BeTrue();

        // Verify via the BlobClient directly.
        var serviceClient = new BlobServiceClient(_azurite.GetConnectionString());
        var blobClient = serviceClient
            .GetBlobContainerClient("test-blobs")
            .GetBlobClient(saveResult.Value);

        var properties = await blobClient.GetPropertiesAsync();
        properties.Value.ContentType.Should().Be(expectedContentType);
    }

    [Fact]
    public async Task RoundTrip_SaveGetDelete_WorksEndToEnd()
    {
        // Full save → get → delete round-trip in a single test.
        const string text = "Integration test content — round-trip";
        using var uploadStream = new MemoryStream(Encoding.UTF8.GetBytes(text));

        // Save
        var saveResult = await _storage.SaveAsync(uploadStream, "round-trip.txt", "text/plain");
        saveResult.IsSuccess.Should().BeTrue();
        var key = saveResult.Value;

        // Get
        var getResult = await _storage.GetAsync(key);
        getResult.IsSuccess.Should().BeTrue();
        await using var downloadedStream = getResult.Value;
        using var reader = new StreamReader(downloadedStream, Encoding.UTF8);
        var downloaded = await reader.ReadToEndAsync();
        downloaded.Should().Be(text);

        // Delete
        var deleteResult = await _storage.DeleteAsync(key);
        deleteResult.IsSuccess.Should().BeTrue();

        // Confirm gone
        var afterDelete = await _storage.GetAsync(key);
        afterDelete.IsFailure.Should().BeTrue();
    }
}
