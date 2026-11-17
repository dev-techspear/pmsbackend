using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PMS.Application.Common;
using PMS.Application.Common.Interfaces;
using PMS.Application.DTOs.Requests;
using PMS.Application.DTOs.Responses;
using PMS.Application.Services.Interfaces;
using PMS.Domain.Constants;
using PMS.Domain.Entities;
using Task = System.Threading.Tasks.Task;

namespace PMS.Application.Services.Implementations;

/// <summary>
/// Credentials stored against a project. Secrets are encrypted before they touch the database,
/// are never part of a list/update response, and leave the API only through <see cref="RevealSecretAsync"/>,
/// which is access-checked and written to an audit trail.
/// </summary>
public class CredentialService : BaseService<CredentialService>, ICredentialService
{
    private const int MaxSecretLength = 4000;

    private readonly IApplicationDbContext _context;
    private readonly IAccessScope _scope;
    private readonly ICredentialProtector _protector;

    public CredentialService(ILogger<CredentialService> logger, IApplicationDbContext context,
        IAccessScope scope, ICredentialProtector protector) : base(logger)
    {
        _context = context;
        _scope = scope;
        _protector = protector;
    }

    public async Task<IReadOnlyList<CredentialResponse>> GetCredentialsAsync(int projectId)
    {
        return await ExecuteAsync(async () =>
        {
            await _context.EnsureProjectAccessAsync(_scope, projectId);
            var items = await _context.ProjectCredentials.ScopeCredentials(_scope)
                .Include(c => c.RoleAccess).Include(c => c.UserAccess)
                .Where(c => c.ProjectId == projectId)
                .OrderBy(c => c.Label).ToListAsync();
            return (IReadOnlyList<CredentialResponse>)items.Select(c => Map(c, _scope)).ToList();
        }, nameof(GetCredentialsAsync));
    }

    public async Task<CredentialResponse> CreateCredentialAsync(int projectId, CreateCredentialRequest request)
    {
        return await ExecuteAsync(async () =>
        {
            RequireCreatePermission();
            await _context.EnsureProjectAccessAsync(_scope, projectId);
            if (!await _context.Projects.AnyAsync(p => p.Id == projectId))
                throw new InvalidOperationException("Project not found.");
            var uid = _scope.UserId ?? throw new AccessDeniedException();

            ValidateFields(request.Label, request.CredentialType, request.Secret, request.Notes);
            var roleIds = request.RestrictAccess ? (request.RoleIds ?? []).Distinct().ToList() : new List<int>();
            var userIds = request.RestrictAccess ? (request.UserIds ?? []).Distinct().ToList() : new List<int>();
            await ValidateShareTargetsAsync(roleIds, userIds);

            var credential = new ProjectCredential
            {
                ProjectId = projectId,
                Label = request.Label.Trim(),
                CredentialType = request.CredentialType,
                Host = Blank(request.Host),
                Username = Blank(request.Username),
                Notes = Blank(request.Notes),
                SecretEncrypted = _protector.Protect(request.Secret),
                RestrictAccess = request.RestrictAccess,
                CreatedByUserId = uid,
                LastRotatedAt = DateTime.UtcNow,
            };
            foreach (var r in roleIds) credential.RoleAccess.Add(new ProjectCredentialRoleAccess { RoleId = r });
            foreach (var u in userIds) credential.UserAccess.Add(new ProjectCredentialUserAccess { UserId = u });
            _context.ProjectCredentials.Add(credential);
            await _context.SaveChangesAsync();

            await LogAsync(credential.Id, uid, AppConstants.CredentialActionCreate);
            return Map(credential, _scope);
        }, nameof(CreateCredentialAsync));
    }

    public async Task<CredentialResponse?> UpdateCredentialAsync(int projectId, int credentialId, UpdateCredentialRequest request)
    {
        return await ExecuteAsync(async () =>
        {
            var credential = await FindVisibleAsync(projectId, credentialId, tracked: true);
            if (credential == null) return null;
            if (!CanManage(credential, _scope))
                throw new AccessDeniedException("Only the creator or an Admin can change this credential.");
            var uid = _scope.UserId ?? throw new AccessDeniedException();

            if (request.Label != null || request.CredentialType != null || request.Secret != null || request.Notes != null)
                ValidateFields(request.Label ?? credential.Label, request.CredentialType ?? credential.CredentialType,
                    request.Secret ?? "x", request.Notes);

            if (request.Label != null) credential.Label = request.Label.Trim();
            if (request.CredentialType != null) credential.CredentialType = request.CredentialType;
            if (request.Host != null) credential.Host = Blank(request.Host);
            if (request.Username != null) credential.Username = Blank(request.Username);
            if (request.Notes != null) credential.Notes = Blank(request.Notes);

            var rotated = false;
            if (!string.IsNullOrEmpty(request.Secret))
            {
                credential.SecretEncrypted = _protector.Protect(request.Secret);
                credential.LastRotatedAt = DateTime.UtcNow;
                rotated = true;
            }

            if (request.RestrictAccess != null || request.RoleIds != null || request.UserIds != null)
            {
                var restrict = request.RestrictAccess ?? credential.RestrictAccess;
                var roleIds = restrict ? (request.RoleIds ?? credential.RoleAccess.Select(r => r.RoleId).ToList()).Distinct().ToList() : new List<int>();
                var userIds = restrict ? (request.UserIds ?? credential.UserAccess.Select(u => u.UserId).ToList()).Distinct().ToList() : new List<int>();
                await ValidateShareTargetsAsync(roleIds, userIds);
                credential.RestrictAccess = restrict;
                _context.ProjectCredentialRoleAccesses.RemoveRange(credential.RoleAccess);
                _context.ProjectCredentialUserAccesses.RemoveRange(credential.UserAccess);
                credential.RoleAccess = roleIds.Select(r => new ProjectCredentialRoleAccess { CredentialId = credential.Id, RoleId = r }).ToList();
                credential.UserAccess = userIds.Select(u => new ProjectCredentialUserAccess { CredentialId = credential.Id, UserId = u }).ToList();
            }

            credential.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            if (rotated) await LogAsync(credential.Id, uid, AppConstants.CredentialActionRotate);
            return Map(credential, _scope);
        }, nameof(UpdateCredentialAsync));
    }

    public async Task<bool> DeleteCredentialAsync(int projectId, int credentialId)
    {
        return await ExecuteAsync(async () =>
        {
            var credential = await FindVisibleAsync(projectId, credentialId, tracked: true);
            if (credential == null) return false;
            if (!CanManage(credential, _scope))
                throw new AccessDeniedException("Only the creator or an Admin can delete this credential.");
            credential.IsDeleted = true;
            await _context.SaveChangesAsync();
            return true;
        }, nameof(DeleteCredentialAsync));
    }

    public async Task<CredentialSecretResponse?> RevealSecretAsync(int projectId, int credentialId)
    {
        return await ExecuteAsync(async () =>
        {
            var credential = await FindVisibleAsync(projectId, credentialId, tracked: false);
            if (credential == null) return null;
            var uid = _scope.UserId ?? throw new AccessDeniedException();

            // Written before the secret is returned: an audit row exists for every successful reveal.
            await LogAsync(credential.Id, uid, AppConstants.CredentialActionReveal);
            return new CredentialSecretResponse(_protector.Unprotect(credential.SecretEncrypted));
        }, nameof(RevealSecretAsync));
    }

    // ---------- helpers ----------

    private async Task<ProjectCredential?> FindVisibleAsync(int projectId, int credentialId, bool tracked)
    {
        await _context.EnsureProjectAccessAsync(_scope, projectId);
        var query = _context.ProjectCredentials.ScopeCredentials(_scope)
            .Include(c => c.RoleAccess).Include(c => c.UserAccess)
            .Where(c => c.Id == credentialId && c.ProjectId == projectId);
        return await (tracked ? query : query.AsNoTracking()).FirstOrDefaultAsync();
    }

    private void RequireCreatePermission()
    {
        if (_scope.IsAdmin || _scope.Level(AppConstants.PermProjectEdit) != null) return;
        throw new AccessDeniedException("You do not have permission to store credentials.");
    }

    private static void ValidateFields(string label, string type, string secret, string? notes)
    {
        if (string.IsNullOrWhiteSpace(label)) throw new InvalidOperationException("Label is required.");
        if (label.Trim().Length > 200) throw new InvalidOperationException("Label is too long (max 200).");
        if (!AppConstants.CredentialTypes.Contains(type)) throw new InvalidOperationException("Unknown credential type.");
        if (string.IsNullOrEmpty(secret)) throw new InvalidOperationException("Secret is required.");
        if (secret.Length > MaxSecretLength) throw new InvalidOperationException($"Secret is too long (max {MaxSecretLength}).");
        if (notes is { Length: > 2000 }) throw new InvalidOperationException("Notes are too long (max 2000).");
    }

    private async Task ValidateShareTargetsAsync(List<int> roleIds, List<int> userIds)
    {
        if (roleIds.Count > 0 && await _context.Roles.CountAsync(r => roleIds.Contains(r.Id)) != roleIds.Count)
            throw new InvalidOperationException("One or more selected roles do not exist.");
        if (userIds.Count > 0 && await _context.Users.CountAsync(u => userIds.Contains(u.Id)) != userIds.Count)
            throw new InvalidOperationException("One or more selected users do not exist.");
    }

    private async Task LogAsync(int credentialId, int userId, string action)
    {
        _context.ProjectCredentialAccessLogs.Add(new ProjectCredentialAccessLog
        {
            CredentialId = credentialId, UserId = userId, Action = action,
        });
        await _context.SaveChangesAsync();
    }

    private static string? Blank(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private static bool CanManage(ProjectCredential c, IAccessScope s) => s.IsAdmin || c.CreatedByUserId == s.UserId;

    private static CredentialResponse Map(ProjectCredential c, IAccessScope s) => new(
        c.Id, c.ProjectId, c.Label, c.CredentialType, c.Host, c.Username, c.Notes, c.RestrictAccess,
        c.RoleAccess.Select(r => r.RoleId).ToList(), c.UserAccess.Select(u => u.UserId).ToList(),
        c.LastRotatedAt, c.CreatedAt, CanManage(c, s));
}
