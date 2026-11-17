using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PMS.Application.DTOs.Requests;
using PMS.Application.Services.Interfaces;
using PMS.Domain.Constants;

namespace PMS.API.Controllers;

[Route("api/v1/documents")]
[Authorize]
public class DocumentsController : BaseController
{
    private const long MaxRequestBytes = AppConstants.DocumentMaxBytes + 1024 * 1024;

    private readonly IDocumentService _documentService;
    private readonly IDocumentFileService _files;

    public DocumentsController(IDocumentService documentService, IDocumentFileService files)
    {
        _documentService = documentService;
        _files = files;
    }

    /// <summary>
    /// Real upload. Form fields: project_id, file, title?, description?, restrict_access (bool),
    /// role_ids[] and user_ids[] (who the file is shared with when restrict_access is true).
    /// </summary>
    [HttpPost("upload")]
    [RequestSizeLimit(MaxRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxRequestBytes)]
    public async Task<IActionResult> Upload(
        [FromForm(Name = "project_id")] int projectId,
        [FromForm(Name = "title")] string? title,
        [FromForm(Name = "description")] string? description,
        [FromForm(Name = "restrict_access")] bool restrictAccess,
        [FromForm(Name = "role_ids")] List<int>? roleIds,
        [FromForm(Name = "user_ids")] List<int>? userIds,
        IFormFile? file)
    {
        if (file == null || file.Length == 0) return BadRequestResult("Choose a file to upload.");
        if (file.Length > AppConstants.DocumentMaxBytes)
            return BadRequestResult($"The file is larger than {AppConstants.DocumentMaxBytes / (1024 * 1024)} MB.");
        try
        {
            await using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            var result = await _files.UploadDocumentAsync(
                new UploadDocumentRequest(projectId, title, description, restrictAccess, roleIds ?? [], userIds ?? []),
                new DocumentUploadFile(file.FileName, file.ContentType, ms.ToArray()));
            return CreatedResult(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequestResult(ex.Message);
        }
    }

    /// <summary>Returns a short-lived read URL (JSON, so the browser's auth header is used to ask for it).</summary>
    [HttpGet("{documentId:int}/download")]
    public async Task<IActionResult> Download(int documentId, [FromQuery] bool inline = false)
    {
        var result = await _files.GetDownloadAsync(documentId, inline);
        if (result == null) return NotFoundResult("Document not found.");
        return OkResult(result);
    }

    [HttpPut("{documentId:int}/sharing")]
    public async Task<IActionResult> UpdateSharing(int documentId, [FromBody] DocumentSharingRequest request)
    {
        try
        {
            var result = await _files.UpdateSharingAsync(documentId, request);
            if (result == null) return NotFoundResult("Document not found.");
            return OkResult(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequestResult(ex.Message);
        }
    }

    // NOTE: this is metadata CRUD only. No Azure Blob Storage upload/download integration
    // exists yet — `file_url` is taken as-is from the request body, nothing is uploaded by
    // this API. See root PROJECT_OVERVIEW.md / MODULE_STATUS.md.
    [HttpPost]
    public async Task<IActionResult> CreateDocument([FromBody] CreateDocumentRequest request)
    {
        var actorId = CurrentUserId;
        var result = await _documentService.CreateDocumentAsync(request, actorId);
        return CreatedResult(result);
    }

    [HttpGet]
    public async Task<IActionResult> ReadDocuments([FromQuery(Name = "project_id")] int? projectId = null)
    {
        var result = await _documentService.GetDocumentsAsync(projectId);
        return OkResult(result);
    }

    [HttpGet("{documentId:int}")]
    public async Task<IActionResult> ReadDocument(int documentId)
    {
        var result = await _documentService.GetDocumentByIdAsync(documentId);
        if (result == null) return NotFoundResult("Document not found.");
        return OkResult(result);
    }

    [HttpPut("{documentId:int}")]
    public async Task<IActionResult> UpdateDocument(int documentId, [FromBody] DocumentUpdateRequest request)
    {
        var actorId = CurrentUserId;
        var result = await _documentService.UpdateDocumentAsync(documentId, request, actorId);
        if (result == null) return NotFoundResult("Document not found.");
        return OkResult(result);
    }

    [HttpDelete("{documentId:int}")]
    public async Task<IActionResult> DeleteDocument(int documentId)
    {
        var actorId = CurrentUserId;
        var success = await _documentService.DeleteDocumentAsync(documentId, actorId);
        if (!success) return NotFoundResult("Document not found.");
        return NoContentResult();
    }
}
