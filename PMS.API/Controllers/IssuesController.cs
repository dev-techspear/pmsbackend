using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PMS.Application.DTOs.Requests;
using PMS.Application.Services.Interfaces;

namespace PMS.API.Controllers;

[Route("api/v1/issues")]
[Authorize]
public class IssuesController : BaseController
{
    private readonly IIssueService _issueService;

    public IssuesController(IIssueService issueService)
    {
        _issueService = issueService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateIssue([FromBody] CreateIssueRequest request)
    {
        var actorId = CurrentUserId;
        var result = await _issueService.CreateIssueAsync(request, actorId);
        return CreatedResult(result);
    }

    [HttpGet("search")]
    public async Task<IActionResult> SearchIssues([FromQuery] string? q, [FromQuery] int limit = 20)
    {
        var result = await _issueService.SearchIssuesAsync(q ?? string.Empty, limit);
        return OkResult(result);
    }

    [HttpGet]
    public async Task<IActionResult> ReadIssues(
        [FromQuery] int skip = 0,
        [FromQuery] int limit = 20,
        [FromQuery(Name = "project_id")] int? projectId = null,
        [FromQuery(Name = "status_id")] int? statusId = null,
        [FromQuery(Name = "assignee_id")] int? assigneeId = null,
        [FromQuery(Name = "severity_id")] int? severityId = null)
    {
        var result = await _issueService.GetIssuesAsync(skip, limit, projectId, statusId, assigneeId, severityId);
        return OkResult(result);
    }

    [HttpGet("{issueId:int}")]
    public async Task<IActionResult> ReadIssue(int issueId)
    {
        var result = await _issueService.GetIssueByIdAsync(issueId);
        if (result == null) return NotFoundResult("Issue not found.");
        return OkResult(result);
    }

    [HttpPut("{issueId:int}")]
    public async Task<IActionResult> UpdateIssue(int issueId, [FromBody] IssueUpdateRequest request)
    {
        var actorId = CurrentUserId;
        var result = await _issueService.UpdateIssueAsync(issueId, request, actorId);
        if (result == null) return NotFoundResult("Issue not found.");
        return OkResult(result);
    }

    [HttpDelete("{issueId:int}")]
    public async Task<IActionResult> DeleteIssue(int issueId)
    {
        var actorId = CurrentUserId;
        var success = await _issueService.DeleteIssueAsync(issueId, actorId);
        if (!success) return NotFoundResult("Issue not found.");
        return NoContentResult();
    }
}
