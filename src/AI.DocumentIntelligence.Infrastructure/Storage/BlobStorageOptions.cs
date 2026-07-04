namespace AI.DocumentIntelligence.Infrastructure.Storage;

/// <summary>
/// Configuration options for Azure Blob Storage. Bound from the
/// <c>AzureStorage</c> section of <c>appsettings.json</c> or environment variables.
/// </summary>
/// <remarks>
/// Set <see cref="ConnectionString"/> to a non-empty value to activate
/// <see cref="AzureBlobFileStorage"/> (production). When the property is empty or absent,
/// the container falls back to <see cref="LocalFileStorage"/> (development default).
///
/// Azurite local emulator connection string:
/// <code>
/// DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;
/// AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;
/// BlobEndpoint=http://azurite:10000/devstoreaccount1;
/// </code>
/// </remarks>
internal sealed class BlobStorageOptions
{
    public const string SectionName = "AzureStorage";

    /// <summary>
    /// The Azure Blob Storage connection string (or Azurite dev connection string for local).
    /// Leave empty to use <see cref="LocalFileStorage"/> instead.
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// The blob container name where uploaded documents are stored.
    /// Defaults to <c>documents</c>; override per environment via config.
    /// </summary>
    public string ContainerName { get; set; } = "documents";
}
