using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PMS.Application.DTOs.Requests;
using PMS.Application.Services.Interfaces;

namespace PMS.API.Controllers;

[Route("api/v1/documents")]
[Authorize]
public class DocumentsController : BaseController
{
    private readonly IDocumentService _documentService;

    public DocumentsController(IDocumentService documentService)
    {
        _documentService = documentService;
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
