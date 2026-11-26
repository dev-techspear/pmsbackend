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

internal static class ShareTargets
{
    /// <summary>Roles/users a caller wants to share with must exist; the share lists are ignored unless restricted.</summary>
    public static async Task<(List<int> Roles, List<int> Users)> NormalizeAsync(
        IApplicationDbContext context, bool restrict, IReadOnlyList<int>? roleIds, IReadOnlyList<int>? userIds)
    {
        var roles = restrict ? (roleIds ?? []).Distinct().ToList() : new List<int>();
        var users = restrict ? (userIds ?? []).Distinct().ToList() : new List<int>();
        if (roles.Count > 0 && await context.Roles.CountAsync(r => roles.Contains(r.Id)) != roles.Count)
            throw new InvalidOperationException("One or more selected roles do not exist.");
        if (users.Count > 0 && await context.Users.CountAsync(u => users.Contains(u.Id)) != users.Count)
            throw new InvalidOperationException("One or more selected users do not exist.");
        return (roles, users);
    }

    public static string? DisplayName(User? u) =>
        u == null ? null : (!string.IsNullOrWhiteSpace(u.DisplayName) ? u.DisplayName
            : string.Join(' ', new[] { u.FirstName, u.LastName }.Where(s => !string.IsNullOrWhiteSpace(s))) is { Length: > 0 } n ? n : u.Email);
}

/// <summary>Typed URLs against a project: repo, live site, staging, design, docs.</summary>
public class ProjectLinkService : BaseService<ProjectLinkService>, IProjectLinkService
{
    private readonly IApplicationDbContext _context;
    private readonly IAccessScope _scope;

    public ProjectLinkService(ILogger<ProjectLinkService> logger, IApplicationDbContext context, IAccessScope scope) : base(logger)
    {
        _context = context;
        _scope = scope;
    }

    public async Task<IReadOnlyList<ProjectLinkResponse>> GetLinksAsync(int projectId)
    {
        return await ExecuteAsync(async () =>
        {
            await _context.EnsureProjectAccessAsync(_scope, projectId);
            var items = await _context.ProjectLinks.ScopeLinks(_scope)
                .Include(l => l.RoleAccess).Include(l => l.UserAccess).Include(l => l.CreatedByUser)
                .Where(l => l.ProjectId == projectId)
                .OrderBy(l => l.LinkType).ThenBy(l => l.Title).ToListAsync();
            return (IReadOnlyList<ProjectLinkResponse>)items.Select(l => Map(l, _scope)).ToList();
        }, nameof(GetLinksAsync));
    }

    public async Task<ProjectLinkResponse> CreateLinkAsync(int projectId, CreateProjectLinkRequest request)
    {
        return await ExecuteAsync(async () =>
        {
            if (!_scope.IsAdmin && _scope.Level(AppConstants.PermProjectEdit) == null)
                throw new AccessDeniedException("You do not have permission to add project links.");
            await _context.EnsureProjectAccessAsync(_scope, projectId);
            if (!await _context.Projects.AnyAsync(p => p.Id == projectId))
                throw new InvalidOperationException("Project not found.");
            var uid = _scope.UserId ?? throw new AccessDeniedException();

            var (title, url, type, description) = Validate(request.Title, request.Url, request.LinkType, request.Description);
            var (roles, users) = await ShareTargets.NormalizeAsync(_context, request.RestrictAccess, request.RoleIds, request.UserIds);

            var link = new ProjectLink
            {
                ProjectId = projectId, Title = title, Url = url, LinkType = type, Description = description,
                RestrictAccess = request.RestrictAccess, CreatedByUserId = uid,
            };
            foreach (var r in roles) link.RoleAccess.Add(new ProjectLinkRoleAccess { RoleId = r });
            foreach (var u in users) link.UserAccess.Add(new ProjectLinkUserAccess { UserId = u });
            _context.ProjectLinks.Add(link);
            await _context.SaveChangesAsync();
            link.CreatedByUser = (await _context.Users.FindAsync(uid))!;
            return Map(link, _scope);
        }, nameof(CreateLinkAsync));
    }

    public async Task<ProjectLinkResponse?> UpdateLinkAsync(int projectId, int linkId, UpdateProjectLinkRequest request)
    {
        return await ExecuteAsync(async () =>
        {
            var link = await FindAsync(projectId, linkId);
            if (link == null) return null;
            if (!CanManage(link, _scope)) throw new AccessDeniedException("Only the person who added this link or an Admin can change it.");

            var (title, url, type, description) = Validate(
                request.Title ?? link.Title, request.Url ?? link.Url, request.LinkType ?? link.LinkType,
                request.Description ?? link.Description);
            link.Title = title; link.Url = url; link.LinkType = type; link.Description = description;

            if (request.RestrictAccess != null || request.RoleIds != null || request.UserIds != null)
            {
                var restrict = request.RestrictAccess ?? link.RestrictAccess;
                var (roles, users) = await ShareTargets.NormalizeAsync(_context, restrict,
                    request.RoleIds ?? link.RoleAccess.Select(r => r.RoleId).ToList(),
                    request.UserIds ?? link.UserAccess.Select(u => u.UserId).ToList());
                link.RestrictAccess = restrict;
                _context.ProjectLinkRoleAccesses.RemoveRange(link.RoleAccess);
                _context.ProjectLinkUserAccesses.RemoveRange(link.UserAccess);
                link.RoleAccess = roles.Select(r => new ProjectLinkRoleAccess { LinkId = link.Id, RoleId = r }).ToList();
                link.UserAccess = users.Select(u => new ProjectLinkUserAccess { LinkId = link.Id, UserId = u }).ToList();
            }

            link.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return Map(link, _scope);
        }, nameof(UpdateLinkAsync));
    }

    public async Task<bool> DeleteLinkAsync(int projectId, int linkId)
    {
        return await ExecuteAsync(async () =>
        {
            var link = await FindAsync(projectId, linkId);
            if (link == null) return false;
            if (!CanManage(link, _scope)) throw new AccessDeniedException("Only the person who added this link or an Admin can delete it.");
            link.IsDeleted = true;
            await _context.SaveChangesAsync();
            return true;
        }, nameof(DeleteLinkAsync));
    }

    private async Task<ProjectLink?> FindAsync(int projectId, int linkId)
    {
        await _context.EnsureProjectAccessAsync(_scope, projectId);
        return await _context.ProjectLinks.ScopeLinks(_scope)
            .Include(l => l.RoleAccess).Include(l => l.UserAccess).Include(l => l.CreatedByUser)
            .FirstOrDefaultAsync(l => l.Id == linkId && l.ProjectId == projectId);
    }

    /// <summary>Only absolute http(s) URLs are accepted, which also rules out javascript:/data: links.</summary>
    internal static (string Title, string Url, string Type, string? Description) Validate(string title, string url, string type, string? description)
    {
        if (string.IsNullOrWhiteSpace(title)) throw new InvalidOperationException("Title is required.");
        if (title.Trim().Length > 200) throw new InvalidOperationException("Title is too long (max 200).");
        if (!AppConstants.LinkTypes.Contains(type)) throw new InvalidOperationException("Unknown link type.");
        var trimmed = (url ?? string.Empty).Trim();
        if (trimmed.Length == 0) throw new InvalidOperationException("URL is required.");
        if (trimmed.Length > AppConstants.LinkMaxUrlLength) throw new InvalidOperationException("URL is too long.");
        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri) || !AppConstants.LinkAllowedSchemes.Contains(uri.Scheme))
            throw new InvalidOperationException("URL must be a full http:// or https:// address.");
        if (description is { Length: > 1000 }) throw new InvalidOperationException("Description is too long (max 1000).");
        return (title.Trim(), trimmed, type, string.IsNullOrWhiteSpace(description) ? null : description.Trim());
    }

    private static bool CanManage(ProjectLink l, IAccessScope s) => s.IsAdmin || l.CreatedByUserId == s.UserId;

    private static ProjectLinkResponse Map(ProjectLink l, IAccessScope s) => new(
        l.Id, l.ProjectId, l.Title, l.Url, l.LinkType, l.Description, l.RestrictAccess,
        l.RoleAccess.Select(r => r.RoleId).ToList(), l.UserAccess.Select(u => u.UserId).ToList(),
        ShareTargets.DisplayName(l.CreatedByUser), l.CreatedAt, CanManage(l, s));
}

/// <summary>Free-text notes on a project. Anyone who can see the project may add one; only the author or an Admin edits it.</summary>
public class ProjectNoteService : BaseService<ProjectNoteService>, IProjectNoteService
{
    private readonly IApplicationDbContext _context;
    private readonly IAccessScope _scope;

    public ProjectNoteService(ILogger<ProjectNoteService> logger, IApplicationDbContext context, IAccessScope scope) : base(logger)
    {
        _context = context;
        _scope = scope;
    }

    public async Task<IReadOnlyList<ProjectNoteResponse>> GetNotesAsync(int projectId)
    {
        return await ExecuteAsync(async () =>
        {
            await _context.EnsureProjectAccessAsync(_scope, projectId);
            var items = await _context.ProjectNotes.ScopeNotes(_scope)
                .Include(n => n.RoleAccess).Include(n => n.UserAccess).Include(n => n.CreatedByUser)
                .Where(n => n.ProjectId == projectId)
                .OrderByDescending(n => n.IsPinned).ThenByDescending(n => n.CreatedAt).ToListAsync();
            return (IReadOnlyList<ProjectNoteResponse>)items.Select(n => Map(n, _scope)).ToList();
        }, nameof(GetNotesAsync));
    }

    public async Task<ProjectNoteResponse> CreateNoteAsync(int projectId, CreateProjectNoteRequest request)
    {
        return await ExecuteAsync(async () =>
        {
            await _context.EnsureProjectAccessAsync(_scope, projectId);
            if (!await _context.Projects.AnyAsync(p => p.Id == projectId))
                throw new InvalidOperationException("Project not found.");
            var uid = _scope.UserId ?? throw new AccessDeniedException();

            var (title, body) = Validate(request.Title, request.Body);
            var (roles, users) = await ShareTargets.NormalizeAsync(_context, request.RestrictAccess, request.RoleIds, request.UserIds);

            var note = new ProjectNote
            {
                ProjectId = projectId, Title = title, Body = body, IsPinned = request.IsPinned,
                RestrictAccess = request.RestrictAccess, CreatedByUserId = uid,
            };
            foreach (var r in roles) note.RoleAccess.Add(new ProjectNoteRoleAccess { RoleId = r });
            foreach (var u in users) note.UserAccess.Add(new ProjectNoteUserAccess { UserId = u });
            _context.ProjectNotes.Add(note);
            await _context.SaveChangesAsync();
            note.CreatedByUser = (await _context.Users.FindAsync(uid))!;
            return Map(note, _scope);
        }, nameof(CreateNoteAsync));
    }

    public async Task<ProjectNoteResponse?> UpdateNoteAsync(int projectId, int noteId, UpdateProjectNoteRequest request)
    {
        return await ExecuteAsync(async () =>
        {
            var note = await FindAsync(projectId, noteId);
            if (note == null) return null;
            if (!CanManage(note, _scope)) throw new AccessDeniedException("Only the author of this note or an Admin can change it.");

            var (title, body) = Validate(request.Title ?? note.Title, request.Body ?? note.Body);
            note.Title = title; note.Body = body;
            if (request.IsPinned.HasValue) note.IsPinned = request.IsPinned.Value;

            if (request.RestrictAccess != null || request.RoleIds != null || request.UserIds != null)
            {
                var restrict = request.RestrictAccess ?? note.RestrictAccess;
                var (roles, users) = await ShareTargets.NormalizeAsync(_context, restrict,
                    request.RoleIds ?? note.RoleAccess.Select(r => r.RoleId).ToList(),
                    request.UserIds ?? note.UserAccess.Select(u => u.UserId).ToList());
                note.RestrictAccess = restrict;
                _context.ProjectNoteRoleAccesses.RemoveRange(note.RoleAccess);
                _context.ProjectNoteUserAccesses.RemoveRange(note.UserAccess);
                note.RoleAccess = roles.Select(r => new ProjectNoteRoleAccess { NoteId = note.Id, RoleId = r }).ToList();
                note.UserAccess = users.Select(u => new ProjectNoteUserAccess { NoteId = note.Id, UserId = u }).ToList();
            }

            note.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return Map(note, _scope);
        }, nameof(UpdateNoteAsync));
    }

    public async Task<bool> DeleteNoteAsync(int projectId, int noteId)
    {
        return await ExecuteAsync(async () =>
        {
            var note = await FindAsync(projectId, noteId);
            if (note == null) return false;
            if (!CanManage(note, _scope)) throw new AccessDeniedException("Only the author of this note or an Admin can delete it.");
            note.IsDeleted = true;
            await _context.SaveChangesAsync();
            return true;
        }, nameof(DeleteNoteAsync));
    }

    private async Task<ProjectNote?> FindAsync(int projectId, int noteId)
    {
        await _context.EnsureProjectAccessAsync(_scope, projectId);
        return await _context.ProjectNotes.ScopeNotes(_scope)
            .Include(n => n.RoleAccess).Include(n => n.UserAccess).Include(n => n.CreatedByUser)
            .FirstOrDefaultAsync(n => n.Id == noteId && n.ProjectId == projectId);
    }

    internal static (string? Title, string Body) Validate(string? title, string? body)
    {
        if (string.IsNullOrWhiteSpace(body)) throw new InvalidOperationException("A note cannot be empty.");
        if (body.Length > AppConstants.NoteMaxBodyLength)
            throw new InvalidOperationException($"A note can be at most {AppConstants.NoteMaxBodyLength} characters.");
        if (title is { Length: > 200 }) throw new InvalidOperationException("Title is too long (max 200).");
        return (string.IsNullOrWhiteSpace(title) ? null : title.Trim(), body.Trim());
    }

    private static bool CanManage(ProjectNote n, IAccessScope s) => s.IsAdmin || n.CreatedByUserId == s.UserId;

    private static ProjectNoteResponse Map(ProjectNote n, IAccessScope s) => new(
        n.Id, n.ProjectId, n.Title, n.Body, n.IsPinned, n.RestrictAccess,
        n.RoleAccess.Select(r => r.RoleId).ToList(), n.UserAccess.Select(u => u.UserId).ToList(),
        ShareTargets.DisplayName(n.CreatedByUser), n.CreatedAt, n.UpdatedAt, CanManage(n, s));
}
