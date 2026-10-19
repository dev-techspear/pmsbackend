using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PMS.Application.DTOs.Requests;
using PMS.Application.Services.Interfaces;

namespace PMS.API.Controllers;

// Flat grouping entity. Confirmed dead in the source app itself (the projects.group_id FK
// that once linked here was added then dropped — see SOURCE_DATABASE_SCHEMA.md) — this
// controller exists for completeness, not because anything depends on it today.
[Route("api/v1/project-groups")]
[Authorize]
public class ProjectGroupsController : BaseController
{
    private readonly IProjectGroupService _projectGroupService;

    public ProjectGroupsController(IProjectGroupService projectGroupService)
    {
        _projectGroupService = projectGroupService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateProjectGroup([FromBody] CreateProjectGroupRequest request)
    {
        var actorId = CurrentUserId;
        var result = await _projectGroupService.CreateProjectGroupAsync(request, actorId);
        return CreatedResult(result);
    }

    [HttpGet]
    public async Task<IActionResult> ReadProjectGroups()
    {
        var result = await _projectGroupService.GetProjectGroupsAsync();
        return OkResult(result);
    }

    [HttpGet("{projectGroupId:int}")]
    public async Task<IActionResult> ReadProjectGroup(int projectGroupId)
    {
        var result = await _projectGroupService.GetProjectGroupByIdAsync(projectGroupId);
        if (result == null) return NotFoundResult("Project group not found.");
        return OkResult(result);
    }

    [HttpPut("{projectGroupId:int}")]
    public async Task<IActionResult> UpdateProjectGroup(int projectGroupId, [FromBody] ProjectGroupUpdateRequest request)
    {
        var actorId = CurrentUserId;
        var result = await _projectGroupService.UpdateProjectGroupAsync(projectGroupId, request, actorId);
        if (result == null) return NotFoundResult("Project group not found.");
        return OkResult(result);
    }

    [HttpDelete("{projectGroupId:int}")]
    public async Task<IActionResult> DeleteProjectGroup(int projectGroupId)
    {
        var actorId = CurrentUserId;
        var success = await _projectGroupService.DeleteProjectGroupAsync(projectGroupId, actorId);
        if (!success) return NotFoundResult("Project group not found.");
        return NoContentResult();
    }
}
