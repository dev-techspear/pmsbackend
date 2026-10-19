using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PMS.Application.DTOs.Requests;
using PMS.Application.Services.Interfaces;

namespace PMS.API.Controllers;

[Route("api/v1/timesheets")]
[Authorize]
public class TimesheetsController : BaseController
{
    private readonly ITimesheetService _timesheetService;

    public TimesheetsController(ITimesheetService timesheetService)
    {
        _timesheetService = timesheetService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateTimesheet([FromBody] CreateTimesheetRequest request)
    {
        var actorId = CurrentUserId;
        var result = await _timesheetService.CreateTimesheetAsync(request, actorId);
        return CreatedResult(result);
    }

    [HttpGet]
    public async Task<IActionResult> ReadTimesheets([FromQuery] int? projectId = null, [FromQuery] string? userEmail = null)
    {
        var result = await _timesheetService.GetTimesheetsAsync(projectId, userEmail);
        return OkResult(result);
    }

    [HttpGet("{timesheetId:int}")]
    public async Task<IActionResult> ReadTimesheet(int timesheetId)
    {
        var result = await _timesheetService.GetTimesheetByIdAsync(timesheetId);
        if (result == null) return NotFoundResult("Timesheet not found.");
        return OkResult(result);
    }

    [HttpPut("{timesheetId:int}")]
    public async Task<IActionResult> UpdateTimesheet(int timesheetId, [FromBody] TimesheetUpdateRequest request)
    {
        var actorId = CurrentUserId;
        var result = await _timesheetService.UpdateTimesheetAsync(timesheetId, request, actorId);
        if (result == null) return NotFoundResult("Timesheet not found.");
        return OkResult(result);
    }

    [HttpDelete("{timesheetId:int}")]
    public async Task<IActionResult> DeleteTimesheet(int timesheetId)
    {
        var actorId = CurrentUserId;
        var success = await _timesheetService.DeleteTimesheetAsync(timesheetId, actorId);
        if (!success) return NotFoundResult("Timesheet not found.");
        return NoContentResult();
    }
}
