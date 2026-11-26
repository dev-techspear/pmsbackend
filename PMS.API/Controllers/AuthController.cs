using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PMS.Application.Common;
using PMS.Application.DTOs.Requests;
using PMS.Application.Services.Interfaces;
using PMS.Domain.Constants;

namespace PMS.API.Controllers;

[Route("api/v1/auth")]
[Route("auth")]
public class AuthController : BaseController
{
    private readonly IAuthService _authService;
    private readonly IPasswordResetService _passwordReset;

    public AuthController(IAuthService authService, IPasswordResetService passwordReset)
    {
        _authService = authService;
        _passwordReset = passwordReset;
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
        catch (Exception ex)
        {
            return BadRequestResult(ex.Message);
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

    /// <summary>Always answers the same way for a known or unknown email. 503 only when no mail service is set up.</summary>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [EnableRateLimiting(AppConstants.AuthRateLimitPolicy)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest payload)
    {
        var outcome = await _passwordReset.RequestAsync(payload?.Email);
        if (outcome == ForgotPasswordOutcome.Unavailable)
            return StatusCode(503, ApiResponse<object>.Fail(AppConstants.PasswordResetUnavailableMessage));
        return OkResult(new { message = AppConstants.PasswordResetSentMessage });
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    [EnableRateLimiting(AppConstants.AuthRateLimitPolicy)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest payload)
    {
        try
        {
            await _passwordReset.ResetAsync(payload?.Token, payload?.NewPassword);
            return OkResult(new { message = "Your password has been changed. You can sign in now." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequestResult(ex.Message);
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
