using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PMS.Application.DTOs.Requests;
using PMS.Application.Services.Interfaces;

namespace PMS.API.Controllers;

/// <summary>Typed links (repo, live site, staging, design, docs) stored against a project.</summary>
[Route("api/v1/projects/{projectId:int}/links")]
[Authorize]
public class ProjectLinksController : BaseController
{
    private readonly IProjectLinkService _links;

    public ProjectLinksController(IProjectLinkService links)
    {
        _links = links;
    }

    [HttpGet]
    public async Task<IActionResult> List(int projectId) => OkResult(await _links.GetLinksAsync(projectId));

    [HttpPost]
    public async Task<IActionResult> Create(int projectId, [FromBody] CreateProjectLinkRequest request)
    {
        try { return CreatedResult(await _links.CreateLinkAsync(projectId, request)); }
        catch (InvalidOperationException ex) { return BadRequestResult(ex.Message); }
    }

    [HttpPut("{linkId:int}")]
    public async Task<IActionResult> Update(int projectId, int linkId, [FromBody] UpdateProjectLinkRequest request)
    {
        try
        {
            var result = await _links.UpdateLinkAsync(projectId, linkId, request);
            return result == null ? NotFoundResult("Link not found.") : OkResult(result);
        }
        catch (InvalidOperationException ex) { return BadRequestResult(ex.Message); }
    }

    [HttpDelete("{linkId:int}")]
    public async Task<IActionResult> Delete(int projectId, int linkId)
        => await _links.DeleteLinkAsync(projectId, linkId) ? NoContentResult() : NotFoundResult("Link not found.");
}
