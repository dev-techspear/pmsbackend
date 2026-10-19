using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PMS.Application.DTOs.Requests;
using PMS.Application.Services.Interfaces;

namespace PMS.API.Controllers;

[Route("api/v1/tasks")]
[Authorize]
public class TasksController : BaseController
{
    private readonly ITaskService _taskService;

    public TasksController(ITaskService taskService)
    {
        _taskService = taskService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateTask([FromBody] CreateTaskRequest request)
    {
        var actorId = CurrentUserId;
        var result = await _taskService.CreateTaskAsync(request, actorId);
        return CreatedResult(result);
    }

    [HttpGet("search")]
    public async Task<IActionResult> SearchTasks([FromQuery] string? q, [FromQuery] int limit = 20)
    {
        var result = await _taskService.SearchTasksAsync(q ?? string.Empty, limit);
        return OkResult(result);
    }

    [HttpGet]
    public async Task<IActionResult> ReadTasks(
        [FromQuery] int skip = 0,
        [FromQuery] int limit = 20,
        [FromQuery(Name = "project_id")] int? projectId = null,
        [FromQuery(Name = "task_list_id")] int? taskListId = null,
        [FromQuery(Name = "status_id")] int? statusId = null,
        [FromQuery(Name = "assignee_id")] int? assigneeId = null)
    {
        var result = await _taskService.GetTasksAsync(skip, limit, projectId, taskListId, statusId, assigneeId);
        return OkResult(result);
    }

    [HttpGet("{taskId:int}")]
    public async Task<IActionResult> ReadTask(int taskId)
    {
        var result = await _taskService.GetTaskByIdAsync(taskId);
        if (result == null) return NotFoundResult("Task not found.");
        return OkResult(result);
    }

    [HttpPut("{taskId:int}")]
    public async Task<IActionResult> UpdateTask(int taskId, [FromBody] TaskUpdateRequest request)
    {
        var actorId = CurrentUserId;
        var result = await _taskService.UpdateTaskAsync(taskId, request, actorId);
        if (result == null) return NotFoundResult("Task not found.");
        return OkResult(result);
    }

    [HttpDelete("{taskId:int}")]
    public async Task<IActionResult> DeleteTask(int taskId)
    {
        var actorId = CurrentUserId;
        var success = await _taskService.DeleteTaskAsync(taskId, actorId);
        if (!success) return NotFoundResult("Task not found.");
        return NoContentResult();
    }
}
