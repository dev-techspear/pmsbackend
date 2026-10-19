using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PMS.Application.DTOs.Requests;
using PMS.Application.Services.Interfaces;

namespace PMS.API.Controllers;

// Route is "tasklists" (no hyphen) — matches pmsfrontend's tasklistsService
// (src/api/services/tasklists.service.ts).
[Route("api/v1/tasklists")]
[Authorize]
public class TaskListsController : BaseController
{
    private readonly ITaskListService _taskListService;

    public TaskListsController(ITaskListService taskListService)
    {
        _taskListService = taskListService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateTaskList([FromBody] CreateTaskListRequest request)
    {
        var actorId = CurrentUserId;
        var result = await _taskListService.CreateTaskListAsync(request, actorId);
        return CreatedResult(result);
    }

    [HttpGet]
    public async Task<IActionResult> ReadTaskLists([FromQuery(Name = "project_id")] int? projectId = null)
    {
        var result = await _taskListService.GetTaskListsAsync(projectId);
        return OkResult(result);
    }

    [HttpGet("{taskListId:int}")]
    public async Task<IActionResult> ReadTaskList(int taskListId)
    {
        var result = await _taskListService.GetTaskListByIdAsync(taskListId);
        if (result == null) return NotFoundResult("Task list not found.");
        return OkResult(result);
    }

    [HttpPut("{taskListId:int}")]
    public async Task<IActionResult> UpdateTaskList(int taskListId, [FromBody] TaskListUpdateRequest request)
    {
        var actorId = CurrentUserId;
        var result = await _taskListService.UpdateTaskListAsync(taskListId, request, actorId);
        if (result == null) return NotFoundResult("Task list not found.");
        return OkResult(result);
    }

    [HttpDelete("{taskListId:int}")]
    public async Task<IActionResult> DeleteTaskList(int taskListId)
    {
        var actorId = CurrentUserId;
        var success = await _taskListService.DeleteTaskListAsync(taskListId, actorId);
        if (!success) return NotFoundResult("Task list not found.");
        return NoContentResult();
    }
}
