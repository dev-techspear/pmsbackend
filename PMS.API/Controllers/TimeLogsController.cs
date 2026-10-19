using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PMS.Application.DTOs.Requests;
using PMS.Application.Services.Interfaces;

namespace PMS.API.Controllers;

[Route("api/v1/timelogs")]
[Authorize]
public class TimeLogsController : BaseController
{
    private readonly ITimeLogService _timeLogService;

    public TimeLogsController(ITimeLogService timeLogService)
    {
        _timeLogService = timeLogService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateTimeLog([FromBody] CreateTimeLogRequest request)
    {
        var actorId = CurrentUserId;
        var result = await _timeLogService.CreateTimeLogAsync(request, actorId);
        return CreatedResult(result);
    }

    [HttpGet]
    public async Task<IActionResult> ReadTimeLogs(
        [FromQuery] int skip = 0,
        [FromQuery] int limit = 500,
        [FromQuery(Name = "project_id")] int? projectId = null,
        [FromQuery(Name = "task_id")] int? taskId = null,
        [FromQuery(Name = "user_id")] int? userId = null,
        [FromQuery(Name = "issue_id")] int? issueId = null)
    {
        // pmsfrontend's timelogsService.getTimelogs() types this as a bare TimeLog[], not
        // the {total, items} envelope the other list endpoints use — match it.
        var result = await _timeLogService.GetTimeLogsAsync(skip, limit, projectId, taskId, userId, issueId);
        return OkResult(result.Items);
    }

    [HttpGet("{timeLogId:int}")]
    public async Task<IActionResult> ReadTimeLog(int timeLogId)
    {
        var result = await _timeLogService.GetTimeLogByIdAsync(timeLogId);
        if (result == null) return NotFoundResult("Time log not found.");
        return OkResult(result);
    }

    [HttpPut("{timeLogId:int}")]
    public async Task<IActionResult> UpdateTimeLog(int timeLogId, [FromBody] TimeLogUpdateRequest request)
    {
        var actorId = CurrentUserId;
        var result = await _timeLogService.UpdateTimeLogAsync(timeLogId, request, actorId);
        if (result == null) return NotFoundResult("Time log not found.");
        return OkResult(result);
    }

    [HttpDelete("{timeLogId:int}")]
    public async Task<IActionResult> DeleteTimeLog(int timeLogId)
    {
        var actorId = CurrentUserId;
        var success = await _timeLogService.DeleteTimeLogAsync(timeLogId, actorId);
        if (!success) return NotFoundResult("Time log not found.");
        return NoContentResult();
    }
}
