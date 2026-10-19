using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PMS.Application.DTOs.Requests;
using PMS.Application.Services.Interfaces;

namespace PMS.API.Controllers;

[Route("api/v1/milestones")]
[Authorize]
public class MilestonesController : BaseController
{
    private readonly IMilestoneService _milestoneService;

    public MilestonesController(IMilestoneService milestoneService)
    {
        _milestoneService = milestoneService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateMilestone([FromBody] CreateMilestoneRequest request)
    {
        var actorId = CurrentUserId;
        var result = await _milestoneService.CreateMilestoneAsync(request, actorId);
        return CreatedResult(result);
    }

    [HttpGet("search")]
    public async Task<IActionResult> SearchMilestones([FromQuery] string? q, [FromQuery] int limit = 20)
    {
        var result = await _milestoneService.SearchMilestonesAsync(q ?? string.Empty, limit);
        return OkResult(result);
    }

    [HttpGet]
    public async Task<IActionResult> ReadMilestones(
        [FromQuery] int skip = 0,
        [FromQuery] int limit = 100,
        [FromQuery(Name = "project_id")] int? projectId = null,
        [FromQuery(Name = "status_id")] int? statusId = null)
    {
        // pmsfrontend's milestonesService.getMilestones() types this as a bare Milestone[],
        // not the {total, items} envelope the other list endpoints use — match it.
        var result = await _milestoneService.GetMilestonesAsync(skip, limit, projectId, statusId);
        return OkResult(result.Items);
    }

    [HttpGet("{milestoneId:int}")]
    public async Task<IActionResult> ReadMilestone(int milestoneId)
    {
        var result = await _milestoneService.GetMilestoneByIdAsync(milestoneId);
        if (result == null) return NotFoundResult("Milestone not found.");
        return OkResult(result);
    }

    [HttpPut("{milestoneId:int}")]
    public async Task<IActionResult> UpdateMilestone(int milestoneId, [FromBody] MilestoneUpdateRequest request)
    {
        var actorId = CurrentUserId;
        var result = await _milestoneService.UpdateMilestoneAsync(milestoneId, request, actorId);
        if (result == null) return NotFoundResult("Milestone not found.");
        return OkResult(result);
    }

    [HttpDelete("{milestoneId:int}")]
    public async Task<IActionResult> DeleteMilestone(int milestoneId)
    {
        var actorId = CurrentUserId;
        var success = await _milestoneService.DeleteMilestoneAsync(milestoneId, actorId);
        if (!success) return NotFoundResult("Milestone not found.");
        return NoContentResult();
    }
}
