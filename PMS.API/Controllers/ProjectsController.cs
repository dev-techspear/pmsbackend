using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PMS.Application.DTOs.Requests;
using PMS.Application.Services.Interfaces;

namespace PMS.API.Controllers;

[Route("api/v1/projects")]
[Authorize]
public class ProjectsController : BaseController
{
    private readonly IProjectService _projectService;

    public ProjectsController(IProjectService projectService)
    {
        _projectService = projectService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateProject([FromBody] CreateProjectRequest request)
    {
        if (await _projectService.ProjectNameExistsAsync(request.ProjectName))
        {
            return BadRequestResult("Project name already exists.");
        }
        if (await _projectService.ProjectIdSyncExistsAsync(request.ProjectIdSync))
        {
            return BadRequestResult("project_id_sync already exists.");
        }

        var actorId = CurrentUserId;
        var result = await _projectService.CreateProjectAsync(request, actorId);
        return CreatedResult(result);
    }

    [HttpGet("check-name")]
    public async Task<IActionResult> CheckProjectName(
        [FromQuery(Name = "name")] string projectName,
        [FromQuery(Name = "exclude_project_id")] int? excludeProjectId = null)
    {
        var exists = await _projectService.ProjectNameExistsAsync(projectName, excludeProjectId);
        return OkResult(new { exists });
    }

    [HttpGet("check-sync-id")]
    public async Task<IActionResult> CheckProjectIdSync(
        [FromQuery(Name = "id")] string projectIdSync,
        [FromQuery(Name = "exclude_project_id")] int? excludeProjectId = null)
    {
        var exists = await _projectService.ProjectIdSyncExistsAsync(projectIdSync, excludeProjectId);
        return OkResult(new { exists });
    }

    [HttpGet("search")]
    public async Task<IActionResult> SearchProjects([FromQuery] string? q, [FromQuery] int limit = 20)
    {
        var result = await _projectService.SearchProjectsAsync(q ?? string.Empty, limit);
        return OkResult(result);
    }

    [HttpGet]
    public async Task<IActionResult> ReadProjects(
        [FromQuery] int skip = 0,
        [FromQuery] int limit = 100,
        [FromQuery] string? search = null,
        [FromQuery(Name = "status_id")] int? statusId = null,
        [FromQuery(Name = "is_archived")] bool? isArchived = null)
    {
        var result = await _projectService.GetProjectsAsync(skip, limit, search, statusId, isArchived);
        return OkResult(result);
    }

    [HttpGet("{projectId:int}")]
    public async Task<IActionResult> ReadProject(int projectId)
    {
        var result = await _projectService.GetProjectByIdAsync(projectId);
        if (result == null) return NotFoundResult("Project not found.");
        return OkResult(result);
    }

    [HttpPut("{projectId:int}")]
    public async Task<IActionResult> UpdateProject(int projectId, [FromBody] ProjectUpdateRequest request)
    {
        var actorId = CurrentUserId;
        var result = await _projectService.UpdateProjectAsync(projectId, request, actorId);
        if (result == null) return NotFoundResult("Project not found.");
        return OkResult(result);
    }

    [HttpDelete("{projectId:int}")]
    public async Task<IActionResult> DeleteProject(int projectId)
    {
        var actorId = CurrentUserId;
        var success = await _projectService.DeleteProjectAsync(projectId, actorId);
        if (!success) return NotFoundResult("Project not found.");
        return NoContentResult();
    }

    [HttpPatch("{projectId:int}/archive")]
    public async Task<IActionResult> ArchiveProject(int projectId)
    {
        var actorId = CurrentUserId;
        var result = await _projectService.SetArchivedAsync(projectId, true, actorId);
        if (result == null) return NotFoundResult("Project not found.");
        return OkResult(result);
    }

    [HttpPatch("{projectId:int}/unarchive")]
    public async Task<IActionResult> UnarchiveProject(int projectId)
    {
        var actorId = CurrentUserId;
        var result = await _projectService.SetArchivedAsync(projectId, false, actorId);
        if (result == null) return NotFoundResult("Project not found.");
        return OkResult(result);
    }

    [HttpGet("{projectId:int}/members")]
    public async Task<IActionResult> GetProjectMembers(int projectId)
    {
        var result = await _projectService.GetProjectMembersAsync(projectId);
        return OkResult(result);
    }

    [HttpPost("{projectId:int}/members")]
    public async Task<IActionResult> AddProjectMember(int projectId, [FromBody] ProjectMemberRequest request)
    {
        var actorId = CurrentUserId;
        var result = await _projectService.AddProjectMemberAsync(projectId, request, actorId);
        if (result == null) return BadRequestResult("Could not add member. Check the project/user exist and aren't already a member.");
        return CreatedResult(result);
    }

    [HttpDelete("{projectId:int}/members/{userId:int}")]
    public async Task<IActionResult> RemoveProjectMember(int projectId, int userId)
    {
        var actorId = CurrentUserId;
        var success = await _projectService.RemoveProjectMemberAsync(projectId, userId, actorId);
        if (!success) return NotFoundResult("Member not found on this project.");
        return NoContentResult();
    }

    [HttpPost("{projectId:int}/clone-to-template")]
    public async Task<IActionResult> CloneToTemplate(int projectId, [FromBody] CloneToTemplateRequest request)
    {
        var actorId = CurrentUserId;
        var result = await _projectService.CloneToTemplateAsync(projectId, request, actorId);
        if (result == null) return NotFoundResult("Project not found.");
        return CreatedResult(result);
    }
}
