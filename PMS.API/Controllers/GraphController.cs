using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PMS.Application.Services.Interfaces;

namespace PMS.API.Controllers;

// Backs pmsfrontend's UserAutocomplete, which searches both /users/search (people already
// in the local DB) and this (anyone in the Azure AD tenant, including people who've never
// logged in yet) and merges the two. Needs the same app-only 'User.Read.All' Graph
// permission as POST /users/sync.
[Route("api/v1/graph")]
[Authorize]
public class GraphController : BaseController
{
    private readonly IUserService _userService;

    public GraphController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet("search-users")]
    public async Task<IActionResult> SearchUsers([FromQuery] string? q, [FromQuery] int limit = 10)
    {
        if (string.IsNullOrWhiteSpace(q)) return OkResult(Array.Empty<object>());

        try
        {
            var result = await _userService.SearchAzureUsersAsync(q, limit);
            return OkResult(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequestResult(ex.Message);
        }
    }
}
