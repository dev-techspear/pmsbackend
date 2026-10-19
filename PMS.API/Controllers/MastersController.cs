using Microsoft.AspNetCore.Mvc;
using PMS.Application.DTOs.Requests;
using PMS.Application.DTOs.Responses;
using PMS.Application.Services.Interfaces;

namespace PMS.API.Controllers;

[Route("api/v1/masters")]
public class MastersController : BaseController
{
    private readonly IMasterService _masterService;

    public MastersController(IMasterService masterService)
    {
        _masterService = masterService;
    }

    [HttpGet("lookups/{category}")]
    public async Task<IActionResult> ReadMasterLookups(string category)
    {
        var result = await _masterService.GetMasterLookupsAsync(category);
        return OkResult(result);
    }

    [HttpGet("lookups/{category}/search")]
    public async Task<IActionResult> SearchMasterLookups(string category, [FromQuery] string q, [FromQuery] int limit = 20)
    {
        var result = await _masterService.SearchMasterLookupsAsync(category, q, limit);
        return OkResult(result);
    }

    [HttpGet("user-statuses")]
    public async Task<IActionResult> ReadUserStatuses()
    {
        var result = await _masterService.GetUserStatusesAsync();
        return OkResult(result);
    }

    [HttpGet("user-statuses/search")]
    public async Task<IActionResult> SearchUserStatuses([FromQuery] string q, [FromQuery] int limit = 20)
    {
        var result = await _masterService.SearchUserStatusesAsync(q, limit);
        return OkResult(result);
    }

    [HttpGet("statuses")]
    public async Task<IActionResult> ReadStatuses()
    {
        var result = await _masterService.GetStatusesAsync();
        return OkResult(result);
    }

    [HttpGet("statuses/search")]
    public async Task<IActionResult> SearchStatuses([FromQuery] string q, [FromQuery] int limit = 20)
    {
        var result = await _masterService.SearchStatusesAsync(q, limit);
        return OkResult(result);
    }

    [HttpGet("priorities")]
    public async Task<IActionResult> ReadPriorities()
    {
        var result = await _masterService.GetPrioritiesAsync();
        return OkResult(result);
    }

    [HttpGet("priorities/search")]
    public async Task<IActionResult> SearchPriorities([FromQuery] string q, [FromQuery] int limit = 20)
    {
        var result = await _masterService.SearchPrioritiesAsync(q, limit);
        return OkResult(result);
    }

    [HttpGet("roles")]
    public async Task<IActionResult> ReadRoles()
    {
        var result = await _masterService.GetRolesAsync();
        return OkResult(result);
    }

    [HttpGet("roles/search")]
    public async Task<IActionResult> SearchRoles([FromQuery] string q, [FromQuery] int limit = 20)
    {
        var result = await _masterService.SearchRolesAsync(q, limit);
        return OkResult(result);
    }

    [HttpGet("roles/{roleId:int}")]
    public async Task<IActionResult> ReadRole(int roleId)
    {
        var result = await _masterService.GetRoleByIdAsync(roleId);
        if (result == null) return NotFoundResult("Role not found.");
        return OkResult(result);
    }

    [HttpPost("roles")]
    public async Task<IActionResult> CreateRole([FromBody] RoleCreateRequest request)
    {
        try
        {
            var result = await _masterService.CreateRoleAsync(request);
            return CreatedResult(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequestResult(ex.Message);
        }
    }

    [HttpPut("roles/{roleId:int}")]
    public async Task<IActionResult> UpdateRole(int roleId, [FromBody] RoleUpdateRequest request)
    {
        var result = await _masterService.UpdateRoleAsync(roleId, request);
        if (result == null) return NotFoundResult("Role not found.");
        return OkResult(result);
    }

    [HttpDelete("roles/{roleId:int}")]
    public async Task<IActionResult> DeleteRole(int roleId)
    {
        var success = await _masterService.DeleteRoleAsync(roleId);
        if (!success) return NotFoundResult("Role not found.");
        return OkResult(new { message = "Role deleted successfully" });
    }

    [HttpPost("roles/bulk-permissions")]
    public async Task<IActionResult> UpdateBulkRolePermissions([FromBody] BulkRolePermissionsUpdateRequest request)
    {
        await _masterService.UpdateBulkRolePermissionsAsync(request);
        return OkResult(new { message = "Permissions updated successfully for all roles" });
    }

    [HttpGet("skills")]
    public async Task<IActionResult> ReadSkills()
    {
        var result = await _masterService.GetSkillsAsync();
        return OkResult(result);
    }

    [HttpGet("skills/search")]
    public async Task<IActionResult> SearchSkills([FromQuery] string q, [FromQuery] int limit = 20)
    {
        var result = await _masterService.SearchSkillsAsync(q, limit);
        return OkResult(result);
    }
}
