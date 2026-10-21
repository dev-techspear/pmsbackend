namespace PMS.Application.Common.Interfaces;

/// <summary>
/// Azure Blob Storage access for large binary content (chat attachments today). Callers never
/// get the bytes back through this interface for reading - only a short-lived SAS URL, so the
/// API server is never in the business of streaming file bytes.
/// </summary>
public interface IBlobStorageService
{
    /// <summary>Uploads content and returns the blob's path within the container.</summary>
    Task<string> UploadAsync(string blobPath, byte[] content, string contentType, CancellationToken ct = default);

    /// <summary>A read-only URL for the blob that expires after <paramref name="validFor"/>.</summary>
    string GetReadSasUrl(string blobPath, TimeSpan validFor);
}
