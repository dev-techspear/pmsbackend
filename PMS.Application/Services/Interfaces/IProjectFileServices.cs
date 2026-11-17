using PMS.Application.DTOs.Requests;
using PMS.Application.DTOs.Responses;

namespace PMS.Application.Services.Interfaces;

public interface IDocumentFileService
{
    System.Threading.Tasks.Task<DocumentResponse> UploadDocumentAsync(UploadDocumentRequest request, DocumentUploadFile file);
    System.Threading.Tasks.Task<DocumentResponse?> UpdateSharingAsync(int documentId, DocumentSharingRequest request);
    System.Threading.Tasks.Task<DocumentDownload?> GetDownloadAsync(int documentId, bool inline = false);
}

public interface ICredentialService
{
    System.Threading.Tasks.Task<IReadOnlyList<CredentialResponse>> GetCredentialsAsync(int projectId);
    System.Threading.Tasks.Task<CredentialResponse> CreateCredentialAsync(int projectId, CreateCredentialRequest request);
    System.Threading.Tasks.Task<CredentialResponse?> UpdateCredentialAsync(int projectId, int credentialId, UpdateCredentialRequest request);
    System.Threading.Tasks.Task<bool> DeleteCredentialAsync(int projectId, int credentialId);
    System.Threading.Tasks.Task<CredentialSecretResponse?> RevealSecretAsync(int projectId, int credentialId);
}
