using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PMS.Application.Common;
using PMS.Application.Common.Interfaces;
using PMS.Application.DTOs.Requests;
using PMS.Application.DTOs.Responses;
using PMS.Application.Services.Interfaces;
using PMS.Domain.Constants;
using PMS.Domain.Entities;
using Task = System.Threading.Tasks.Task;

namespace PMS.Application.Services.Implementations;

/// <summary>
/// Real file upload/download for project documents. Bytes go to the shared blob container under
/// AppConstants.DocumentBlobPrefix; the API only ever hands the browser a short-lived read link,
/// and only after the same sharing rules that filter the list (see ScopeDocuments) have passed.
/// </summary>
public class DocumentFileService : BaseService<DocumentFileService>, IDocumentFileService
{
    private readonly IApplicationDbContext _context;
    private readonly IAccessScope _scope;
    private readonly IBlobStorageService _blobs;

    public DocumentFileService(ILogger<DocumentFileService> logger, IApplicationDbContext context,
        IAccessScope scope, IBlobStorageService blobs) : base(logger)
    {
        _context = context;
        _scope = scope;
        _blobs = blobs;
    }

    public async Task<DocumentResponse> UploadDocumentAsync(UploadDocumentRequest request, DocumentUploadFile file)
    {
        return await ExecuteAsync(async () =>
        {
            await _context.EnsureProjectAccessAsync(_scope, request.ProjectId);
            if (!await _context.Projects.AnyAsync(p => p.Id == request.ProjectId))
                throw new InvalidOperationException("Project not found.");

            var fileName = SafeFileName(file.FileName);
            var ext = Path.GetExtension(fileName).ToLowerInvariant();
            if (!AppConstants.DocumentAllowedExtensions.Contains(ext))
                throw new InvalidOperationException($"\"{fileName}\" is not an allowed file type.");
            if (file.Content.Length == 0)
                throw new InvalidOperationException("The file is empty.");
            if (file.Content.Length > AppConstants.DocumentMaxBytes)
                throw new InvalidOperationException($"\"{fileName}\" is larger than {AppConstants.DocumentMaxBytes / (1024 * 1024)} MB.");

            var roleIds = request.RestrictAccess ? request.RoleIds.Distinct().ToList() : new List<int>();
            var userIds = request.RestrictAccess ? request.UserIds.Distinct().ToList() : new List<int>();
            await ValidateShareTargetsAsync(roleIds, userIds);

            var contentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType;
            var blobPath = $"{AppConstants.DocumentBlobPrefix}/{request.ProjectId}/{Guid.NewGuid():N}/{fileName}";
            await _blobs.UploadAsync(blobPath, file.Content, contentType);

            var document = new Document
            {
                PublicId = "DOC-" + Guid.NewGuid().ToString("N")[..8].ToUpper(),
                Title = string.IsNullOrWhiteSpace(request.Title) ? Path.GetFileNameWithoutExtension(fileName) : request.Title.Trim(),
                Description = request.Description,
                FileUrl = blobPath,
                BlobPath = blobPath,
                FileType = contentType,
                FileSize = file.Content.Length,
                ProjectId = request.ProjectId,
                UploadedByEmail = _scope.Email,
                RestrictAccess = request.RestrictAccess,
            };
            foreach (var r in roleIds) document.RoleAccess.Add(new DocumentRoleAccess { RoleId = r });
            foreach (var u in userIds) document.UserAccess.Add(new DocumentUserAccess { UserId = u });

            _context.Documents.Add(document);
            await _context.SaveChangesAsync();
            return DocumentService.MapToResponse(document, _scope);
        }, nameof(UploadDocumentAsync));
    }

    public async Task<DocumentResponse?> UpdateSharingAsync(int documentId, DocumentSharingRequest request)
    {
        return await ExecuteAsync(async () =>
        {
            var document = await _context.Documents.ScopeDocuments(_scope)
                .Include(d => d.RoleAccess).Include(d => d.UserAccess)
                .FirstOrDefaultAsync(d => d.Id == documentId);
            if (document == null) return null;
            if (!DocumentService.CanManage(document, _scope))
                throw new AccessDeniedException("Only the uploader or an Admin can change who can see this document.");

            var roleIds = request.RestrictAccess ? (request.RoleIds ?? []).Distinct().ToList() : new List<int>();
            var userIds = request.RestrictAccess ? (request.UserIds ?? []).Distinct().ToList() : new List<int>();
            await ValidateShareTargetsAsync(roleIds, userIds);

            document.RestrictAccess = request.RestrictAccess;
            _context.DocumentRoleAccesses.RemoveRange(document.RoleAccess);
            _context.DocumentUserAccesses.RemoveRange(document.UserAccess);
            document.RoleAccess = roleIds.Select(r => new DocumentRoleAccess { DocumentId = document.Id, RoleId = r }).ToList();
            document.UserAccess = userIds.Select(u => new DocumentUserAccess { DocumentId = document.Id, UserId = u }).ToList();
            await _context.SaveChangesAsync();
            return DocumentService.MapToResponse(document, _scope);
        }, nameof(UpdateSharingAsync));
    }

    public async Task<DocumentDownload?> GetDownloadAsync(int documentId, bool inline = false)
    {
        return await ExecuteAsync(async () =>
        {
            // ScopeDocuments applies project access AND the sharing rules - a document the caller
            // may not see is indistinguishable from one that does not exist.
            var document = await _context.Documents.ScopeDocuments(_scope)
                .Where(d => d.Id == documentId)
                .Select(d => new { d.Title, d.FileUrl, d.BlobPath })
                .FirstOrDefaultAsync();
            if (document == null) return null;

            if (string.IsNullOrEmpty(document.BlobPath))
                return new DocumentDownload(document.Title, document.FileUrl); // legacy link-only document

            var url = _blobs.GetReadSasUrl(document.BlobPath, TimeSpan.FromMinutes(AppConstants.DocumentSasMinutes),
                inline ? null : Path.GetFileName(document.BlobPath));
            return new DocumentDownload(Path.GetFileName(document.BlobPath), url);
        }, nameof(GetDownloadAsync));
    }

    private async Task ValidateShareTargetsAsync(List<int> roleIds, List<int> userIds)
    {
        if (roleIds.Count > 0 && await _context.Roles.CountAsync(r => roleIds.Contains(r.Id)) != roleIds.Count)
            throw new InvalidOperationException("One or more selected roles do not exist.");
        if (userIds.Count > 0 && await _context.Users.CountAsync(u => userIds.Contains(u.Id)) != userIds.Count)
            throw new InvalidOperationException("One or more selected users do not exist.");
    }

    private static string SafeFileName(string name)
    {
        var clean = Path.GetFileName(name ?? string.Empty).Trim();
        foreach (var c in Path.GetInvalidFileNameChars()) clean = clean.Replace(c, '_');
        return string.IsNullOrWhiteSpace(clean) ? "file" : clean;
    }
}
