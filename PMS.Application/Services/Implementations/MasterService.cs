using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PMS.Application.Common;
using PMS.Application.Common.Interfaces;
using PMS.Application.DTOs.Requests;
using PMS.Application.DTOs.Responses;
using PMS.Application.Services.Interfaces;
using PMS.Domain.Entities;
using System.Text.Json.Nodes;
using Task = System.Threading.Tasks.Task;

namespace PMS.Application.Services.Implementations;

public class MasterService : BaseService<MasterService>, IMasterService
{
    private readonly IApplicationDbContext _context;
    private readonly IChatAccessSync _chatAccess;

    public MasterService(ILogger<MasterService> logger, IApplicationDbContext context, IChatAccessSync chatAccess) : base(logger)
    {
        _context = context;
        _chatAccess = chatAccess;
    }

    public async Task<IEnumerable<MasterLookupResponse>> GetMasterLookupsAsync(string category)
    {
        return await ExecuteAsync(async () =>
        {
            var cleanCat = category.Trim().ToLower();
            var categoriesToCheck = new List<string> { category };

            if (cleanCat == "taskstatus" || cleanCat == "task_status" || cleanCat == "status" || cleanCat == "task status")
            {
                categoriesToCheck = new List<string> { "TaskStatus", "task_status", "Status", "task status" };
            }
            else if (cleanCat == "taskpriority" || cleanCat == "task_priority" || cleanCat == "priority" || cleanCat == "task priority")
            {
                categoriesToCheck = new List<string> { "TaskPriority", "Priority", "task_priority", "priority" };
            }
            else if (cleanCat == "milestonestatus" || cleanCat == "milestone_status")
            {
                categoriesToCheck = new List<string> { "MilestoneStatus", "ProjectStatus", "milestone_status" };
            }
            else if (cleanCat == "issuestatus" || cleanCat == "issue_status")
            {
                categoriesToCheck = new List<string> { "IssueStatus", "issue_status", "Status" };
            }

            var items = await _context.MasterLookups
                .Where(ml => ml.IsActive && 
                             (categoriesToCheck.Contains(ml.Category) || ml.Category.ToLower() == cleanCat))
                .OrderBy(ml => ml.OrderIndex)
                .ThenBy(ml => ml.Id)
                .ToListAsync();

            // Unique items by label (case-insensitive)
            var seen = new HashSet<string>();
            var result = new List<MasterLookupResponse>();

            foreach (var item in items)
            {
                var cleanLabel = item.Label.Trim().ToLower();
                if (!seen.Contains(cleanLabel))
                {
                    result.Add(new MasterLookupResponse(
                        item.Id,
                        item.Category,
                        item.Value,
                        item.Label,
                        item.Color,
                        item.Icon,
                        item.OrderIndex,
                        item.IsActive
                    ));
                    seen.Add(cleanLabel);
                }
            }

            return result.AsEnumerable();
        }, nameof(GetMasterLookupsAsync));
    }

    public async Task<IEnumerable<MasterLookupResponse>> SearchMasterLookupsAsync(string category, string query, int limit)
    {
        return await ExecuteAsync(async () =>
        {
            var items = await _context.MasterLookups
                .Where(ml => ml.Category == category && ml.IsActive && ml.Label.Contains(query))
                .OrderBy(ml => ml.OrderIndex)
                .Take(limit)
                .ToListAsync();

            return items.Select(item => new MasterLookupResponse(
                item.Id,
                item.Category,
                item.Value,
                item.Label,
                item.Color,
                item.Icon,
                item.OrderIndex,
                item.IsActive
            ));
        }, nameof(SearchMasterLookupsAsync));
    }

    public async Task<IEnumerable<MasterResponse>> GetUserStatusesAsync()
    {
        return await ExecuteAsync(async () =>
        {
            var items = await _context.UserStatuses.ToListAsync();
            return items.GroupBy(i => i.Name.Trim().ToLower())
                        .Select(g => g.First())
                        .Select(i => new MasterResponse(i.Id, i.Name));
        }, nameof(GetUserStatusesAsync));
    }

    public async Task<IEnumerable<MasterResponse>> SearchUserStatusesAsync(string query, int limit)
    {
        return await ExecuteAsync(async () =>
        {
            if (string.IsNullOrWhiteSpace(query)) return Enumerable.Empty<MasterResponse>();

            var items = await _context.UserStatuses
                .Where(us => us.Name.Contains(query))
                .Take(limit)
                .ToListAsync();

            return items.Select(i => new MasterResponse(i.Id, i.Name));
        }, nameof(SearchUserStatusesAsync));
    }

    public async Task<IEnumerable<MasterResponse>> GetStatusesAsync()
    {
        return await ExecuteAsync(async () =>
        {
            var items = await _context.UserStatuses.ToListAsync(); // mapped to statuses
            return items.GroupBy(i => i.Name.Trim().ToLower())
                        .Select(g => g.First())
                        .Select(i => new MasterResponse(i.Id, i.Name));
        }, nameof(GetStatusesAsync));
    }

    public async Task<IEnumerable<MasterResponse>> SearchStatusesAsync(string query, int limit)
    {
        return await ExecuteAsync(async () =>
        {
            if (string.IsNullOrWhiteSpace(query)) return Enumerable.Empty<MasterResponse>();

            var items = await _context.UserStatuses
                .Where(s => s.Name.Contains(query))
                .Take(limit)
                .ToListAsync();

            return items.Select(i => new MasterResponse(i.Id, i.Name));
        }, nameof(SearchStatusesAsync));
    }

    public async Task<IEnumerable<MasterResponse>> GetPrioritiesAsync()
    {
        return await ExecuteAsync(async () =>
        {
            var items = await _context.MasterLookups
                .Where(ml => ml.Category == "Priority" || ml.Category == "TaskPriority")
                .ToListAsync();

            return items.GroupBy(i => i.Label.Trim().ToLower())
                        .Select(g => g.First())
                        .Select(i => new MasterResponse(i.Id, i.Label));
        }, nameof(GetPrioritiesAsync));
    }

    public async Task<IEnumerable<MasterResponse>> SearchPrioritiesAsync(string query, int limit)
    {
        return await ExecuteAsync(async () =>
        {
            if (string.IsNullOrWhiteSpace(query)) return Enumerable.Empty<MasterResponse>();

            var items = await _context.MasterLookups
                .Where(ml => (ml.Category == "Priority" || ml.Category == "TaskPriority") && ml.Label.Contains(query))
                .Take(limit)
                .ToListAsync();

            return items.Select(i => new MasterResponse(i.Id, i.Label));
        }, nameof(SearchPrioritiesAsync));
    }

    public async Task<IEnumerable<RoleResponse>> GetRolesAsync()
    {
        return await ExecuteAsync(async () =>
        {
            var roles = await _context.Roles.Include(r => r.Users).ToListAsync();
            var seen = new HashSet<string>();
            var result = new List<RoleResponse>();

            foreach (var role in roles)
            {
                var normName = role.Name.Trim().ToLower();
                if (!seen.Contains(normName))
                {
                    JsonNode? permsNode = null;
                    try
                    {
                        permsNode = JsonNode.Parse(role.Permissions ?? "{}");
                    }
                    catch { permsNode = JsonNode.Parse("{}"); }

                    result.Add(new RoleResponse(
                        role.Id,
                        role.Name,
                        role.Description,
                        permsNode,
                        role.Users.Count
                    ));
                    seen.Add(normName);
                }
            }

            return result.AsEnumerable();
        }, nameof(GetRolesAsync));
    }

    public async Task<IEnumerable<RoleResponse>> SearchRolesAsync(string query, int limit)
    {
        return await ExecuteAsync(async () =>
        {
            if (string.IsNullOrWhiteSpace(query)) return Enumerable.Empty<RoleResponse>();

            var roles = await _context.Roles
                .Include(r => r.Users)
                .Where(r => r.Name.Contains(query))
                .Take(limit)
                .ToListAsync();

            var seen = new HashSet<string>();
            var result = new List<RoleResponse>();

            foreach (var role in roles)
            {
                var normName = role.Name.Trim().ToLower();
                if (!seen.Contains(normName))
                {
                    JsonNode? permsNode = null;
                    try { permsNode = JsonNode.Parse(role.Permissions ?? "{}"); } catch { permsNode = JsonNode.Parse("{}"); }

                    result.Add(new RoleResponse(
                        role.Id,
                        role.Name,
                        role.Description,
                        permsNode,
                        role.Users.Count
                    ));
                    seen.Add(normName);
                }
            }

            return result;
        }, nameof(SearchRolesAsync));
    }

    public async Task<RoleResponse?> GetRoleByIdAsync(int roleId)
    {
        return await ExecuteAsync(async () =>
        {
            var role = await _context.Roles
                .Include(r => r.Users)
                .FirstOrDefaultAsync(r => r.Id == roleId);

            if (role == null) return null;

            JsonNode? permsNode = null;
            try { permsNode = JsonNode.Parse(role.Permissions ?? "{}"); } catch { permsNode = JsonNode.Parse("{}"); }

            return new RoleResponse(
                role.Id,
                role.Name,
                role.Description,
                permsNode,
                role.Users.Count
            );
        }, nameof(GetRoleByIdAsync));
    }

    public async Task<RoleResponse> CreateRoleAsync(RoleCreateRequest request)
    {
        return await ExecuteAsync(async () =>
        {
            var existing = await _context.Roles.FirstOrDefaultAsync(r => r.Name == request.Name);
            if (existing != null)
            {
                throw new InvalidOperationException($"A role named '{request.Name}' already exists.");
            }

            var permissionsJson = request.Permissions?.ToJsonString() ?? "{}";

            var role = new Role
            {
                Name = request.Name,
                Description = request.Description,
                Permissions = permissionsJson
            };

            _context.Roles.Add(role);
            await _context.SaveChangesAsync();

            return new RoleResponse(
                role.Id,
                role.Name,
                role.Description,
                request.Permissions,
                0
            );
        }, nameof(CreateRoleAsync));
    }

    public async Task<RoleResponse?> UpdateRoleAsync(int roleId, RoleUpdateRequest request)
    {
        return await ExecuteAsync(async () =>
        {
            var role = await _context.Roles
                .Include(r => r.Users)
                .FirstOrDefaultAsync(r => r.Id == roleId);

            if (role == null) return null;

            if (request.Name != null) role.Name = request.Name;
            if (request.Description != null) role.Description = request.Description;
            if (request.Permissions != null) role.Permissions = request.Permissions.ToJsonString();

            await _context.SaveChangesAsync();
            await _chatAccess.RevalidateAllAsync();

            JsonNode? permsNode = null;
            try { permsNode = JsonNode.Parse(role.Permissions ?? "{}"); } catch { permsNode = JsonNode.Parse("{}"); }

            return new RoleResponse(
                role.Id,
                role.Name,
                role.Description,
                permsNode,
                role.Users.Count
            );
        }, nameof(UpdateRoleAsync));
    }

    public async Task<bool> DeleteRoleAsync(int roleId)
    {
        return await ExecuteAsync(async () =>
        {
            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Id == roleId);
            if (role == null) return false;

            _context.Roles.Remove(role);
            await _context.SaveChangesAsync();
            await _chatAccess.RevalidateAllAsync();
            return true;
        }, nameof(DeleteRoleAsync));
    }

    public async Task UpdateBulkRolePermissionsAsync(BulkRolePermissionsUpdateRequest request)
    {
        await ExecuteAsync(async () =>
        {
            foreach (var entry in request.RolePermissions)
            {
                var roleId = entry.Key;
                var permsJson = entry.Value.ToJsonString();

                var role = await _context.Roles.FirstOrDefaultAsync(r => r.Id == roleId);
                if (role != null)
                {
                    role.Permissions = permsJson;
                }
            }

            await _context.SaveChangesAsync();
            await _chatAccess.RevalidateAllAsync();
            return true;
        }, nameof(UpdateBulkRolePermissionsAsync));
    }

    public async Task<IEnumerable<SkillResponse>> GetSkillsAsync()
    {
        return await ExecuteAsync(async () =>
        {
            var items = await _context.Skills.ToListAsync();
            return items.GroupBy(i => i.Name.Trim().ToLower())
                        .Select(g => g.First())
                        .Select(i => new SkillResponse(i.Id, i.Name));
        }, nameof(GetSkillsAsync));
    }

    public async Task<IEnumerable<SkillResponse>> SearchSkillsAsync(string query, int limit)
    {
        return await ExecuteAsync(async () =>
        {
            if (string.IsNullOrWhiteSpace(query)) return Enumerable.Empty<SkillResponse>();

            var items = await _context.Skills
                .Where(s => s.Name.Contains(query))
                .Take(limit)
                .ToListAsync();

            return items.Select(i => new SkillResponse(i.Id, i.Name));
        }, nameof(SearchSkillsAsync));
    }
}
