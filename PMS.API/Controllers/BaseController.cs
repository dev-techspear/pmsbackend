using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using PMS.Application.Common;

namespace PMS.API.Controllers;

[ApiController]
[Produces("application/json")]
public abstract class BaseController : ControllerBase
{
    // Current user helpers extracted from JWT
    protected string CurrentUserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new UnauthorizedAccessException("User identity not found in token.");

    protected string? CurrentUserIdOrNull => User.FindFirstValue(ClaimTypes.NameIdentifier);
    protected string CurrentUserRole => User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
    protected string CurrentUserEmail => User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;

    // Unified success envelope returns (Http 200, 201, 204)
    protected IActionResult OkResult<T>(T data) => Ok(ApiResponse<T>.Success(data));
    protected IActionResult CreatedResult<T>(T data, string? location = null) =>
        location is null ? StatusCode(201, ApiResponse<T>.Success(data)) : Created(location, ApiResponse<T>.Success(data));
    protected IActionResult NoContentResult() => NoContent();

    // Unified error response mappings (Http 400, 401, 403, 404, 409)
    protected IActionResult BadRequestResult(string message) => BadRequest(ApiResponse<object>.Fail(message));
    protected IActionResult BadRequestResult(IEnumerable<string> errors) => BadRequest(ApiResponse<object>.Fail("Validation failed.", errors));
    protected IActionResult NotFoundResult(string message) => NotFound(ApiResponse<object>.Fail(message));
    protected IActionResult ForbiddenResult(string message = "Access denied.") => StatusCode(403, ApiResponse<object>.Fail(message));
    protected IActionResult ConflictResult(string message) => Conflict(ApiResponse<object>.Fail(message));
    protected IActionResult UnauthorizedResult(string message = "Unauthorized.") => Unauthorized(ApiResponse<object>.Fail(message));
}
