using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.Extensions.Configuration;
using PMS.Application.Common.Interfaces;

namespace PMS.Infrastructure.Services;

public class BlobStorageService : IBlobStorageService
{
    private readonly IConfiguration _configuration;
    private readonly Lazy<BlobContainerClient> _container;

    public BlobStorageService(IConfiguration configuration)
    {
        _configuration = configuration;
        // Lazy: a chat text message with no attachment never touches Azure, and the app must
        // keep running (and every other chat action must keep working) before the storage
        // account exists - this only throws once something actually uploads/downloads a file.
        _container = new Lazy<BlobContainerClient>(BuildContainerClient);
    }

    private BlobContainerClient BuildContainerClient()
    {
        var connectionString = _configuration["AzureBlobStorage:ConnectionString"];
        if (string.IsNullOrWhiteSpace(connectionString) || connectionString == "CHANGEME")
            throw new InvalidOperationException(
                "AzureBlobStorage:ConnectionString is not configured. Set it in appsettings.json " +
                "(or user-secrets) once the storage account exists - see appsettings.Example.json.");
        var containerName = _configuration["AzureBlobStorage:ContainerName"] ?? "chat-attachments";

        // An account-key connection string gives BlobClient.GenerateSasUri everything it needs
        // to sign SAS URLs itself - no need to hold or parse the key separately here.
        var serviceClient = new BlobServiceClient(connectionString);
        return serviceClient.GetBlobContainerClient(containerName);
    }

    public async Task<string> UploadAsync(string blobPath, byte[] content, string contentType, CancellationToken ct = default)
    {
        var container = _container.Value;
        await container.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: ct);
        var blob = container.GetBlobClient(blobPath);
        using var stream = new MemoryStream(content);
        await blob.UploadAsync(stream, new BlobHttpHeaders { ContentType = contentType }, cancellationToken: ct);
        return blobPath;
    }

    public string GetReadSasUrl(string blobPath, TimeSpan validFor)
    {
        var container = _container.Value;
        var blob = container.GetBlobClient(blobPath);
        var builder = new BlobSasBuilder
        {
            BlobContainerName = container.Name,
            BlobName = blobPath,
            Resource = "b",
            ExpiresOn = DateTimeOffset.UtcNow.Add(validFor),
        };
        builder.SetPermissions(BlobSasPermissions.Read);
        return blob.GenerateSasUri(builder).ToString();
    }
}
