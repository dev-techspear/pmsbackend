using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PMS.Application.DTOs.Requests;
using PMS.Application.Services.Interfaces;
using System.Threading.Tasks;

namespace PMS.API.Controllers;

[Route("api/v1/teams")]
[Authorize]
public class TeamsController : BaseController
{
    private readonly ITeamService _teamService;

    public TeamsController(ITeamService teamService)
    {
        _teamService = teamService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateTeam([FromBody] TeamCreateRequest request)
    {
        var actorId = CurrentUserId;
        var result = await _teamService.CreateTeamAsync(request, actorId);
        return CreatedResult(result);
    }

    [HttpGet]
    public async Task<IActionResult> ReadTeams([FromQuery] int skip = 0, [FromQuery] int limit = 100)
    {
        // pmsfrontend's teamsService.getTeams() types this as a bare Team[] and uses it
        // directly (Teams.tsx calls .filter()/.reduce() on the result) — match it, same
        // as users/milestones/timelogs/templates.
        var result = await _teamService.GetTeamsAsync(skip, limit);
        return OkResult(result.Items);
    }

    [HttpGet("search")]
    public async Task<IActionResult> SearchTeams([FromQuery] string? q, [FromQuery] int limit = 20)
    {
        var result = await _teamService.SearchTeamsAsync(q ?? string.Empty, limit);
        return OkResult(result);
    }

    [HttpGet("{teamId:int}")]
    public async Task<IActionResult> ReadTeam(int teamId)
    {
        var result = await _teamService.GetTeamByIdAsync(teamId);
        if (result == null)
        {
            return NotFoundResult("Team not found.");
        }

        return OkResult(result);
    }

    [HttpPut("{teamId:int}")]
    public async Task<IActionResult> UpdateTeam(int teamId, [FromBody] TeamUpdateRequest request)
    {
        var actorId = CurrentUserId;
        var result = await _teamService.UpdateTeamAsync(teamId, request, actorId);
        if (result == null)
        {
            return NotFoundResult("Team not found.");
        }

        return OkResult(result);
    }

    [HttpDelete("{teamId:int}")]
    public async Task<IActionResult> DeleteTeam(int teamId)
    {
        var actorId = CurrentUserId;
        var success = await _teamService.DeleteTeamAsync(teamId, actorId);
        if (!success)
        {
            return NotFoundResult("Team not found.");
        }

        return NoContentResult();
    }

    [HttpPost("{teamId:int}/members/{userEmail}")]
    public async Task<IActionResult> AddTeamMember(int teamId, string userEmail)
    {
        var actorId = CurrentUserId;
        var success = await _teamService.AddTeamMemberAsync(teamId, userEmail, actorId);
        if (!success)
        {
            return BadRequestResult("Could not add user to team. Check if user and team exist.");
        }

        return OkResult(new { message = "Member added successfully" });
    }

    [HttpDelete("{teamId:int}/members/{userEmail}")]
    public async Task<IActionResult> RemoveTeamMember(int teamId, string userEmail)
    {
        var actorId = CurrentUserId;
        var success = await _teamService.RemoveTeamMemberAsync(teamId, userEmail, actorId);
        if (!success)
        {
            return BadRequestResult("Could not remove user from team. Check if user is in team.");
        }

        return OkResult(new { message = "Member removed successfully" });
    }
}
