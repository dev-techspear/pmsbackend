using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PMS.Application.DTOs.Requests;
using PMS.Application.Services.Interfaces;

namespace PMS.API.Controllers;

// Route is "templates", not "project-templates" — matches what pmsfrontend's
// projectsService already calls (src/api/services/projects.service.ts).
[Route("api/v1/templates")]
[Authorize]
public class ProjectTemplatesController : BaseController
{
    private readonly IProjectTemplateService _templateService;

    public ProjectTemplatesController(IProjectTemplateService templateService)
    {
        _templateService = templateService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateTemplate([FromBody] CreateProjectTemplateRequest request)
    {
        var actorId = CurrentUserId;
        var result = await _templateService.CreateTemplateAsync(request, actorId);
        return CreatedResult(result);
    }

    [HttpGet]
    public async Task<IActionResult> ReadTemplates([FromQuery] int skip = 0, [FromQuery] int limit = 100)
    {
        // pmsfrontend's projectsService.getTemplates() types this as a bare ProjectTemplate[],
        // not the {total, items} envelope every other list endpoint uses — match it.
        var result = await _templateService.GetTemplatesAsync(skip, limit);
        return OkResult(result.Items);
    }

    [HttpGet("{templateId:int}")]
    public async Task<IActionResult> ReadTemplate(int templateId)
    {
        var result = await _templateService.GetTemplateByIdAsync(templateId);
        if (result == null) return NotFoundResult("Template not found.");
        return OkResult(result);
    }

    [HttpPut("{templateId:int}")]
    public async Task<IActionResult> UpdateTemplate(int templateId, [FromBody] ProjectTemplateUpdateRequest request)
    {
        var actorId = CurrentUserId;
        var result = await _templateService.UpdateTemplateAsync(templateId, request, actorId);
        if (result == null) return NotFoundResult("Template not found.");
        return OkResult(result);
    }

    [HttpDelete("{templateId:int}")]
    public async Task<IActionResult> DeleteTemplate(int templateId)
    {
        var actorId = CurrentUserId;
        var success = await _templateService.DeleteTemplateAsync(templateId, actorId);
        if (!success) return NotFoundResult("Template not found.");
        return NoContentResult();
    }

    [HttpPost("{templateId:int}/tasks")]
    public async Task<IActionResult> AddTask(int templateId, [FromBody] CreateTemplateTaskRequest request)
    {
        var result = await _templateService.AddTaskAsync(templateId, request);
        if (result == null) return NotFoundResult("Template not found.");
        return CreatedResult(result);
    }

    [HttpDelete("{templateId:int}/tasks/{taskId:int}")]
    public async Task<IActionResult> RemoveTask(int templateId, int taskId)
    {
        var success = await _templateService.RemoveTaskAsync(templateId, taskId);
        if (!success) return NotFoundResult("Template task not found.");
        return NoContentResult();
    }
}
