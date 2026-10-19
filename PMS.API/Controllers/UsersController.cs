using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PMS.Application.DTOs.Requests;
using PMS.Application.Services.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PMS.API.Controllers;

[Route("api/v1/users")]
[Authorize]
public class UsersController : BaseController
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet("me")]
    public async Task<IActionResult> ReadUserMe()
    {
        if (!int.TryParse(CurrentUserId, out int userId))
        {
            return UnauthorizedResult("User identity is invalid.");
        }

        var profile = await _userService.GetUserByIdAsync(userId);
        if (profile == null)
        {
            return NotFoundResult("User profile not found.");
        }

        return OkResult(profile);
    }

    [HttpGet("search")]
    public async Task<IActionResult> SearchUsers([FromQuery] string? q, [FromQuery] int limit = 20, [FromQuery] bool? isActive = true)
    {
        var result = await _userService.SearchUsersAsync(q, limit, isActive);
        return OkResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] UserCreateRequest request)
    {
        var emailExists = await _userService.GetUserByEmailAsync(request.Email);
        if (emailExists != null)
        {
            return BadRequestResult("Email already registered.");
        }

        if (!string.IsNullOrEmpty(request.Username))
        {
            var usernameExists = await _userService.GetUserByUsernameAsync(request.Username);
            if (usernameExists != null)
            {
                return BadRequestResult("Username already registered.");
            }
        }

        var actorId = CurrentUserId;
        var result = await _userService.CreateUserAsync(request, actorId);
        return CreatedResult(result);
    }

    [HttpPost("sync")]
    public async Task<IActionResult> SyncUsersFromAzure()
    {
        try
        {
            var result = await _userService.SyncUsersFromAzureAsync();
            return OkResult(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequestResult(ex.Message);
        }
    }

    [HttpGet]
    public async Task<IActionResult> ReadUsers(
        [FromQuery] int skip = 0,
        [FromQuery] int limit = 100,
        [FromQuery] string? search = null,
        [FromQuery] List<int>? roleId = null,
        [FromQuery] bool? isActive = null)
    {
        // pmsfrontend's usersService.getUsers() types this as a bare User[] (with a
        // defensive .data fallback that this {total, items} shape doesn't match either) —
        // match it, same as templates/milestones/timelogs.
        var result = await _userService.GetUsersAsync(skip, limit, search, roleId, isActive);
        return OkResult(result.Items);
    }

    [HttpGet("{userId:int}")]
    public async Task<IActionResult> ReadUser(int userId)
    {
        var result = await _userService.GetUserByIdAsync(userId);
        if (result == null)
        {
            return NotFoundResult("User not found.");
        }

        return OkResult(result);
    }

    [HttpPut("{userId:int}")]
    public async Task<IActionResult> UpdateUser(int userId, [FromBody] UserUpdateRequest request)
    {
        var actorId = CurrentUserId;
        var result = await _userService.UpdateUserAsync(userId, request, actorId);
        if (result == null)
        {
            return NotFoundResult("User not found.");
        }

        return OkResult(result);
    }

    [HttpDelete("{userId:int}")]
    public async Task<IActionResult> DeleteUser(int userId)
    {
        var actorId = CurrentUserId;
        var success = await _userService.DeleteUserAsync(userId, actorId);
        if (!success)
        {
            return NotFoundResult("User not found.");
        }

        return NoContentResult();
    }
}
