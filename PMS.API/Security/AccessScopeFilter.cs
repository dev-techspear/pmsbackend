using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using PMS.Application.Common;
using PMS.Application.Common.Interfaces;
using PMS.Domain.Constants;

namespace PMS.API.Security;

/// <summary>
/// Runs for every authenticated action. It loads the caller's access (role permissions) once per
/// request so services can filter by it, and for EXTERNAL users it also enforces role permissions
/// at the endpoint: they get nothing the role does not grant. Internal users keep their existing
/// behaviour here (their view levels still filter data inside the services).
/// </summary>
public class AccessScopeFilter : IAsyncActionFilter
{
    private readonly IAccessScope _scope;

    public AccessScopeFilter(IAccessScope scope)
    {
        _scope = scope;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var allowsAnonymous = context.ActionDescriptor.EndpointMetadata.OfType<IAllowAnonymous>().Any();
        var idClaim = context.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!allowsAnonymous && int.TryParse(idClaim, out var userId))
        {
            var tokenPermissions = TokenPermissions(context.HttpContext.User);
            if (tokenPermissions is not null)
            {
                var roleName = context.HttpContext.User.FindFirstValue(ClaimTypes.Role);
                var email = context.HttpContext.User.FindFirstValue(ClaimTypes.Email);
                var external = string.Equals(context.HttpContext.User.FindFirstValue("utype"), "EXTERNAL", StringComparison.OrdinalIgnoreCase);
                _scope.ApplyTokenPermissions(userId, email, roleName, external, tokenPermissions);
            }
            else
            {
                await _scope.LoadAsync(userId);

            // PMS-issued tokens (no "iss") from before a password reset are refused. Tokens from the Spear ID
            // hub are not PMS's to revoke.
            var principal = context.HttpContext.User;
            if (_scope.IsLoaded && principal.FindFirst("iss") == null
                && SessionRevocation.IsRevoked(_scope.SessionsValidFrom, principal.FindFirstValue("iat")))
            {
                context.Result = new ObjectResult(ApiResponse<object>.Fail(AppConstants.SessionEndedMessage))
                {
                    StatusCode = StatusCodes.Status401Unauthorized,
                };
                return;
            }
            }

            if (_scope.IsExternal && !_scope.IsAdmin)
            {
                var controller = (context.ActionDescriptor as ControllerActionDescriptor)?.ControllerName ?? "";
                var request = context.HttpContext.Request;
                var required = ExternalPermissionMap.RequiredPermission(controller, request.Method, request.Path.Value ?? "");

                if (required == ExternalPermissionMap.Allowed)
                {
                    // explicitly open to any signed-in user (e.g. /auth/me, lookup data)
                }
                else if (required == null || _scope.Level(required) == null)
                {
                    context.Result = new ObjectResult(ApiResponse<object>.Fail("Access denied."))
                    {
                        StatusCode = StatusCodes.Status403Forbidden,
                    };
                    return;
                }
            }
        }

        await next();
    }

    /// <summary>Null when this session was not issued by Spear ID, so the local role JSON still applies.</summary>
    private static IReadOnlyList<string>? TokenPermissions(ClaimsPrincipal user)
    {
        var values = user.FindAll("perm").Select(c => c.Value).Where(v => !string.IsNullOrWhiteSpace(v)).ToList();
        if (values.Count == 0) return null;
        if (values.Count == 1 && values[0].TrimStart().StartsWith('['))
        {
            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<List<string>>(values[0]) ?? [];
            }
            catch (System.Text.Json.JsonException)
            {
                return [];
            }
        }
        return values;
    }
}

public class AccessDeniedExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is not AccessDeniedException ex) return;
        context.Result = new ObjectResult(ApiResponse<object>.Fail(ex.Message))
        {
            StatusCode = StatusCodes.Status403Forbidden,
        };
        context.ExceptionHandled = true;
    }
}

/// <summary>
/// Which role permission an external user needs for an endpoint. Deny by default: a controller not
/// listed here is closed to external users.
/// </summary>
public static class ExternalPermissionMap
{
    public const string Allowed = "*";

    private static readonly Dictionary<string, string> Prefix = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Projects"] = "proj",
        ["ProjectGroups"] = "proj",
        ["ProjectTemplates"] = "proj",
        ["Documents"] = "proj",
        ["Tasks"] = "task",
        ["TaskLists"] = "task",
        ["Issues"] = "issue",
        ["Milestones"] = "milestone",
        ["TimeLogs"] = "time",
        ["Timesheets"] = "time",
        ["Teams"] = "team",
    };

    public static string? RequiredPermission(string controller, string method, string path)
    {
        if (controller.Equals("Auth", StringComparison.OrdinalIgnoreCase)) return Allowed;

        var isRead = HttpMethods.IsGet(method) || HttpMethods.IsHead(method);

        // Lookup data (statuses, priorities, roles list ...) feeds every dropdown; read-only.
        if (controller.Equals("Masters", StringComparison.OrdinalIgnoreCase)) return isRead ? Allowed : null;

        // Own profile is always readable; the directory itself needs user-view/-create/-edit/-delete
        // (granted per role like any other permission), and syncing from Azure AD is an admin-only
        // automation action, not something user-view should ever unlock.
        if (controller.Equals("Users", StringComparison.OrdinalIgnoreCase))
        {
            var trimmed = path.TrimEnd('/');
            if (isRead && trimmed.EndsWith("/users/me", StringComparison.OrdinalIgnoreCase)) return Allowed;
            if (trimmed.EndsWith("/users/sync", StringComparison.OrdinalIgnoreCase)) return AppConstants.PermAutomationManage;
            if (isRead) return AppConstants.PermUserView;
            if (HttpMethods.IsPost(method)) return AppConstants.PermUserCreate;
            if (HttpMethods.IsDelete(method)) return AppConstants.PermUserDelete;
            return AppConstants.PermUserEdit;
        }

        // Project chat: reading (and marking read) needs chat-view, writing needs chat-post.
        if (controller.Equals("Chat", StringComparison.OrdinalIgnoreCase))
        {
            var trimmed = path.TrimEnd('/');
            return isRead || trimmed.EndsWith("/read", StringComparison.OrdinalIgnoreCase)
                          || trimmed.EndsWith("/ticket", StringComparison.OrdinalIgnoreCase)
                ? AppConstants.PermChatView
                : AppConstants.PermChatPost;
        }

        // Project credentials: seeing the list or revealing a secret needs proj-view (the per-credential
        // sharing rules then decide which ones); storing, rotating or deleting needs proj-edit.
        if (controller.Equals("Credentials", StringComparison.OrdinalIgnoreCase))
        {
            return isRead || path.TrimEnd('/').EndsWith("/reveal", StringComparison.OrdinalIgnoreCase)
                ? AppConstants.PermProjectView
                : AppConstants.PermProjectEdit;
        }

        // Project links: reading needs proj-view; adding/editing/removing needs proj-edit.
        if (controller.Equals("ProjectLinks", StringComparison.OrdinalIgnoreCase))
            return isRead ? AppConstants.PermProjectView : AppConstants.PermProjectEdit;

        // Project notes: anyone who can see the project may add one; the service limits edit/delete
        // to the author (or an Admin).
        if (controller.Equals("ProjectNotes", StringComparison.OrdinalIgnoreCase))
            return AppConstants.PermProjectView;

        if (!Prefix.TryGetValue(controller, out var prefix)) return null;

        var action =
            isRead ? "view"
            : HttpMethods.IsPost(method) ? "create"
            : HttpMethods.IsDelete(method) ? "delete"
            : "edit";

        // Changing a project's membership or a project document is editing the project.
        if (!isRead && (path.Contains("/members", StringComparison.OrdinalIgnoreCase)
                        || controller.Equals("Documents", StringComparison.OrdinalIgnoreCase)))
        {
            return "proj-edit";
        }

        return $"{prefix}-{action}";
    }
}
