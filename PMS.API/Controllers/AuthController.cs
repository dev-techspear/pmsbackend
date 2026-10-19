using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PMS.Application.DTOs.Requests;
using PMS.Application.Services.Interfaces;
using PMS.Domain.Constants;

namespace PMS.API.Controllers;

[Route("api/v1/auth")]
public class AuthController : BaseController
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("redirect")]
    [AllowAnonymous]
    public async Task<IActionResult> MicrosoftCallback([FromBody] MSCallbackRequest payload)
    {
        try
        {
            var response = await _authService.HandleMicrosoftCallbackAsync(payload);
            return OkResult(response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequestResult(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            return ForbiddenResult(ex.Message);
        }
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(AppConstants.AuthRateLimitPolicy)]
    public async Task<IActionResult> Login([FromBody] LoginRequest payload)
    {
        try
        {
            var response = await _authService.LoginExternalAsync(payload);
            return OkResult(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return UnauthorizedResult(ex.Message);
        }
    }

    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting(AppConstants.AuthRateLimitPolicy)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest payload)
    {
        try
        {
            var response = await _authService.RegisterExternalAsync(payload);
            return CreatedResult(response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequestResult(ex.Message);
        }
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUserProfile()
    {
        if (!int.TryParse(CurrentUserId, out int userId))
        {
            return UnauthorizedResult("User identity is invalid.");
        }

        var profile = await _authService.GetUserProfileAsync(userId);
        if (profile == null)
        {
            return NotFoundResult("User profile not found.");
        }

        return OkResult(profile);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest payload)
    {
        try
        {
            var response = await _authService.RefreshTokenAsync(payload);
            return OkResult(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return UnauthorizedResult(ex.Message);
        }
    }
}
