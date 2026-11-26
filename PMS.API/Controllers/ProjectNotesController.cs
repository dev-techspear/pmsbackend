using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PMS.Application.DTOs.Requests;
using PMS.Application.Services.Interfaces;

namespace PMS.API.Controllers;

/// <summary>Free-text notes on a project.</summary>
[Route("api/v1/projects/{projectId:int}/notes")]
[Authorize]
public class ProjectNotesController : BaseController
{
    private readonly IProjectNoteService _notes;

    public ProjectNotesController(IProjectNoteService notes)
    {
        _notes = notes;
    }

    [HttpGet]
    public async Task<IActionResult> List(int projectId) => OkResult(await _notes.GetNotesAsync(projectId));

    [HttpPost]
    public async Task<IActionResult> Create(int projectId, [FromBody] CreateProjectNoteRequest request)
    {
        try { return CreatedResult(await _notes.CreateNoteAsync(projectId, request)); }
        catch (InvalidOperationException ex) { return BadRequestResult(ex.Message); }
    }

    [HttpPut("{noteId:int}")]
    public async Task<IActionResult> Update(int projectId, int noteId, [FromBody] UpdateProjectNoteRequest request)
    {
        try
        {
            var result = await _notes.UpdateNoteAsync(projectId, noteId, request);
            return result == null ? NotFoundResult("Note not found.") : OkResult(result);
        }
        catch (InvalidOperationException ex) { return BadRequestResult(ex.Message); }
    }

    [HttpDelete("{noteId:int}")]
    public async Task<IActionResult> Delete(int projectId, int noteId)
        => await _notes.DeleteNoteAsync(projectId, noteId) ? NoContentResult() : NotFoundResult("Note not found.");
}
