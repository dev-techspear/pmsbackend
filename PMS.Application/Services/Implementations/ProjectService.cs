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

public class ProjectService : BaseService<ProjectService>, IProjectService
{
    private readonly IApplicationDbContext _context;
    private readonly IAccessScope _scope;
    private readonly IChatAccessSync _chatAccess;

    public ProjectService(ILogger<ProjectService> logger, IApplicationDbContext context, IAccessScope scope, IChatAccessSync chatAccess) : base(logger)
    {
        _context = context;
        _scope = scope;
        _chatAccess = chatAccess;
    }

    private IQueryable<Project> IncludeGraph(IQueryable<Project> query, bool scoped = true) =>
        (scoped ? query.ScopeProjects(_scope) : query)
        .Include(p => p.StatusMaster)
        .Include(p => p.PriorityMaster)
        .Include(p => p.Owner)
        .Include(p => p.ProjectManager)
        .Include(p => p.DeliveryHead)
        .Include(p => p.TeamMembers).ThenInclude(m => m.User);

    private readonly record struct ChildCounts(int Tasks, int Issues, int Milestones);

    // Grouped COUNT queries rather than `.Include(p => p.Tasks)` etc. — avoids pulling
    // every task/issue/milestone row into memory just to count them, and works for both
    // a single project and a whole page of them in one round trip each.
    private async Task<Dictionary<int, ChildCounts>> GetChildCountsAsync(IEnumerable<int> projectIds)
    {
        var ids = projectIds.Distinct().ToList();
        if (ids.Count == 0) return new Dictionary<int, ChildCounts>();

        var taskCounts = await _context.Tasks
            .Where(t => t.ProjectId.HasValue && ids.Contains(t.ProjectId.Value))
            .GroupBy(t => t.ProjectId!.Value)
            .Select(g => new { ProjectId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ProjectId, x => x.Count);

        var issueCounts = await _context.Issues
            .Where(i => i.ProjectId.HasValue && ids.Contains(i.ProjectId.Value))
            .GroupBy(i => i.ProjectId!.Value)
            .Select(g => new { ProjectId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ProjectId, x => x.Count);

        var milestoneCounts = await _context.Milestones
            .Where(m => m.ProjectId.HasValue && ids.Contains(m.ProjectId.Value))
            .GroupBy(m => m.ProjectId!.Value)
            .Select(g => new { ProjectId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ProjectId, x => x.Count);

        return ids.ToDictionary(id => id, id => new ChildCounts(
            taskCounts.GetValueOrDefault(id, 0),
            issueCounts.GetValueOrDefault(id, 0),
            milestoneCounts.GetValueOrDefault(id, 0)
        ));
    }

    public async Task<ProjectResponse> CreateProjectAsync(CreateProjectRequest request, string actorId)
    {
        return await ExecuteAsync(async () =>
        {
            var publicId = "PRJ-" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();

            var project = new Project
            {
                PublicId = publicId,
                ProjectIdSync = request.ProjectIdSync,
                AccountName = request.AccountName,
                ProjectName = request.ProjectName,
                CustomerName = request.CustomerName,
                ClientName = request.ClientName,
                BillingModel = request.BillingModel,
                ProjectType = request.ProjectType,
                ProjectStatusExternal = request.ProjectStatusExternal,
                ExpectedStartDate = request.ExpectedStartDate,
                ExpectedEndDate = request.ExpectedEndDate,
                Description = request.Description,
                Tags = request.Tags,
                // A restricted creator would otherwise not be able to see the project they just made.
                OwnerId = request.OwnerId ?? (_scope.IsRestricted(AppConstants.PermProjectView) && int.TryParse(actorId, out var creatorId) ? creatorId : null),
                ProjectManagerId = request.ProjectManagerId,
                DeliveryHeadId = request.DeliveryHeadId,
                StatusId = request.StatusId,
                PriorityId = request.PriorityId,
                EstimatedHours = request.EstimatedHours ?? 0m,
                ActualHours = request.ActualHours ?? 0m,
                ActualStartDate = request.ActualStartDate,
                ActualEndDate = request.ActualEndDate,
            };

            if (request.UserEmails != null && request.UserEmails.Any())
            {
                var members = await _context.Users
                    .Where(u => request.UserEmails.Contains(u.Email))
                    .ToListAsync();
                foreach (var member in members)
                {
                    project.TeamMembers.Add(new ProjectMember { UserId = member.Id, IsOwner = false });
                }
            }

            _context.Projects.Add(project);
            await _context.SaveChangesAsync();

            var reloaded = await IncludeGraph(_context.Projects, scoped: false).FirstAsync(p => p.Id == project.Id);
            return MapToProjectResponse(reloaded);
        }, nameof(CreateProjectAsync));
    }

    public async Task<ProjectListResponseDto> GetProjectsAsync(int skip, int limit, string? search, int? statusId, bool? isArchived)
    {
        return await ExecuteAsync(async () =>
        {
            var queryable = IncludeGraph(_context.Projects).AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.ToLower();
                queryable = queryable.Where(p =>
                    p.ProjectName.ToLower().Contains(term) ||
                    p.AccountName.ToLower().Contains(term) ||
                    p.CustomerName.ToLower().Contains(term));
            }
            if (statusId.HasValue)
            {
                queryable = queryable.Where(p => p.StatusId == statusId);
            }
            if (isArchived.HasValue)
            {
                queryable = queryable.Where(p => p.IsArchived == isArchived.Value);
            }

            var total = await queryable.CountAsync();
            var items = await queryable
                .OrderByDescending(p => p.Id)
                .Skip(skip)
                .Take(limit)
                .ToListAsync();

            var counts = await GetChildCountsAsync(items.Select(p => p.Id));
            return new ProjectListResponseDto(total, items.Select(p =>
            {
                var c = counts.GetValueOrDefault(p.Id, default(ChildCounts));
                return MapToProjectResponse(p, c.Tasks, c.Issues, c.Milestones);
            }));
        }, nameof(GetProjectsAsync));
    }

    public async Task<IEnumerable<ProjectResponse>> SearchProjectsAsync(string query, int limit)
    {
        return await ExecuteAsync(async () =>
        {
            var queryable = IncludeGraph(_context.Projects).AsQueryable();

            if (!string.IsNullOrWhiteSpace(query))
            {
                var term = query.ToLower();
                queryable = queryable.Where(p => p.ProjectName.ToLower().Contains(term));
            }

            var items = await queryable.OrderBy(p => p.ProjectName).Take(limit).ToListAsync();
            var counts = await GetChildCountsAsync(items.Select(p => p.Id));
            return items.Select(p =>
            {
                var c = counts.GetValueOrDefault(p.Id, default(ChildCounts));
                return MapToProjectResponse(p, c.Tasks, c.Issues, c.Milestones);
            });
        }, nameof(SearchProjectsAsync));
    }

    public async Task<ProjectResponse?> GetProjectByIdAsync(int projectId)
    {
        return await ExecuteAsync(async () =>
        {
            var project = await IncludeGraph(_context.Projects).FirstOrDefaultAsync(p => p.Id == projectId);
            if (project == null) return null;
            var c = (await GetChildCountsAsync([projectId])).GetValueOrDefault(projectId, default(ChildCounts));
            return MapToProjectResponse(project, c.Tasks, c.Issues, c.Milestones);
        }, nameof(GetProjectByIdAsync));
    }

    public async Task<bool> ProjectNameExistsAsync(string projectName, int? excludeProjectId = null) =>
        await _context.Projects.AnyAsync(p =>
            p.ProjectName == projectName && (!excludeProjectId.HasValue || p.Id != excludeProjectId.Value));

    public async Task<bool> ProjectIdSyncExistsAsync(string projectIdSync, int? excludeProjectId = null) =>
        await _context.Projects.AnyAsync(p =>
            p.ProjectIdSync == projectIdSync && (!excludeProjectId.HasValue || p.Id != excludeProjectId.Value));

    public async Task<ProjectResponse?> UpdateProjectAsync(int projectId, ProjectUpdateRequest request, string actorId)
    {
        return await ExecuteAsync(async () =>
        {
            var project = await IncludeGraph(_context.Projects).FirstOrDefaultAsync(p => p.Id == projectId);
            if (project == null) return null;

            if (request.AccountName != null) project.AccountName = request.AccountName;
            if (request.ProjectName != null) project.ProjectName = request.ProjectName;
            if (request.CustomerName != null) project.CustomerName = request.CustomerName;
            if (request.ClientName != null) project.ClientName = request.ClientName;
            if (request.Tags != null) project.Tags = request.Tags;
            if (request.BillingModel != null) project.BillingModel = request.BillingModel;
            if (request.ProjectType != null) project.ProjectType = request.ProjectType;
            if (request.ProjectStatusExternal != null) project.ProjectStatusExternal = request.ProjectStatusExternal;
            if (request.ExpectedStartDate.HasValue) project.ExpectedStartDate = request.ExpectedStartDate;
            if (request.ExpectedEndDate.HasValue) project.ExpectedEndDate = request.ExpectedEndDate;
            if (request.OwnerId.HasValue) project.OwnerId = request.OwnerId;
            if (request.ProjectManagerId.HasValue) project.ProjectManagerId = request.ProjectManagerId;
            if (request.DeliveryHeadId.HasValue) project.DeliveryHeadId = request.DeliveryHeadId;
            if (request.StatusId.HasValue)
            {
                project.PreviousStatusId = project.StatusId;
                project.StatusId = request.StatusId;
            }
            if (request.PriorityId.HasValue) project.PriorityId = request.PriorityId;
            if (request.Description != null) project.Description = request.Description;
            if (request.EstimatedHours.HasValue) project.EstimatedHours = request.EstimatedHours.Value;
            if (request.ActualHours.HasValue) project.ActualHours = request.ActualHours.Value;
            if (request.ActualStartDate.HasValue) project.ActualStartDate = request.ActualStartDate;
            if (request.ActualEndDate.HasValue) project.ActualEndDate = request.ActualEndDate;

            if (request.UserEmails != null)
            {
                var members = await _context.Users
                    .Where(u => request.UserEmails.Contains(u.Email))
                    .ToListAsync();
                project.TeamMembers.Clear();
                foreach (var member in members)
                {
                    project.TeamMembers.Add(new ProjectMember { ProjectId = projectId, UserId = member.Id });
                }
            }

            await _context.SaveChangesAsync();
            // Owner / manager / delivery head / members may have changed: kick out anyone who lost the room.
            await _chatAccess.RevalidateProjectAsync(projectId);

            var reloaded = await IncludeGraph(_context.Projects).FirstAsync(p => p.Id == projectId);
            var c = (await GetChildCountsAsync([projectId])).GetValueOrDefault(projectId, default(ChildCounts));
            return MapToProjectResponse(reloaded, c.Tasks, c.Issues, c.Milestones);
        }, nameof(UpdateProjectAsync));
    }

    public async Task<bool> DeleteProjectAsync(int projectId, string actorId)
    {
        return await ExecuteAsync(async () =>
        {
            var project = await _context.Projects.ScopeProjects(_scope).FirstOrDefaultAsync(p => p.Id == projectId);
            if (project == null) return false;

            // Soft delete — matches the AuditMixin/is_deleted pattern used throughout the
            // source schema rather than a hard DELETE (see guard-tests/db-migration rules:
            // hard deletes here would also hit the NoAction FKs intentionally left in place
            // to dodge SQL Server's multi-cascade-path restriction).
            project.IsDeleted = true;
            await _context.SaveChangesAsync();
            await _chatAccess.RevalidateProjectAsync(projectId);
            return true;
        }, nameof(DeleteProjectAsync));
    }

    public async Task<ProjectResponse?> SetArchivedAsync(int projectId, bool isArchived, string actorId)
    {
        return await ExecuteAsync(async () =>
        {
            var project = await IncludeGraph(_context.Projects).FirstOrDefaultAsync(p => p.Id == projectId);
            if (project == null) return null;

            project.IsArchived = isArchived;
            await _context.SaveChangesAsync();
            var c = (await GetChildCountsAsync([projectId])).GetValueOrDefault(projectId, default(ChildCounts));
            return MapToProjectResponse(project, c.Tasks, c.Issues, c.Milestones);
        }, nameof(SetArchivedAsync));
    }

    public async Task<IEnumerable<ProjectMemberResponseDto>> GetProjectMembersAsync(int projectId)
    {
        return await ExecuteAsync(async () =>
        {
            if (!await _context.Projects.ScopeProjects(_scope).AnyAsync(p => p.Id == projectId))
                return Enumerable.Empty<ProjectMemberResponseDto>();

            var members = await _context.ProjectMembers
                .Include(m => m.User)
                .Where(m => m.ProjectId == projectId)
                .ToListAsync();
            return members.Select(MapToProjectMemberResponse);
        }, nameof(GetProjectMembersAsync));
    }

    public async Task<ProjectMemberResponseDto?> AddProjectMemberAsync(int projectId, ProjectMemberRequest request, string actorId)
    {
        return await ExecuteAsync(async () =>
        {
            var project = await _context.Projects.ScopeProjects(_scope).FirstOrDefaultAsync(p => p.Id == projectId);
            if (project == null) return null;

            User? user = null;
            if (request.UserId.HasValue)
            {
                user = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.UserId.Value);
            }
            else if (!string.IsNullOrWhiteSpace(request.UserEmail))
            {
                user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.UserEmail);
            }
            if (user == null) return null;

            var existing = await _context.ProjectMembers
                .FirstOrDefaultAsync(m => m.ProjectId == projectId && m.UserId == user.Id);
            if (existing != null) return null;

            var member = new ProjectMember
            {
                ProjectId = projectId,
                UserId = user.Id,
                ProjectProfile = request.ProjectProfile,
                PortalProfile = request.PortalProfile,
            };
            _context.ProjectMembers.Add(member);
            await _context.SaveChangesAsync();

            member.User = user;
            return MapToProjectMemberResponse(member);
        }, nameof(AddProjectMemberAsync));
    }

    public async Task<bool> RemoveProjectMemberAsync(int projectId, int userId, string actorId)
    {
        return await ExecuteAsync(async () =>
        {
            if (!await _context.Projects.ScopeProjects(_scope).AnyAsync(p => p.Id == projectId)) return false;

            var member = await _context.ProjectMembers
                .FirstOrDefaultAsync(m => m.ProjectId == projectId && m.UserId == userId);
            if (member == null) return false;

            _context.ProjectMembers.Remove(member);
            await _context.SaveChangesAsync();
            await _chatAccess.RevalidateProjectAsync(projectId);
            return true;
        }, nameof(RemoveProjectMemberAsync));
    }

    public async Task<ProjectTemplateResponse?> CloneToTemplateAsync(int projectId, CloneToTemplateRequest request, string actorId)
    {
        return await ExecuteAsync(async () =>
        {
            var project = await _context.Projects.ScopeProjects(_scope)
                .Include(p => p.Tasks)
                .FirstOrDefaultAsync(p => p.Id == projectId);
            if (project == null) return null;

            int? createdById = int.TryParse(actorId, out var aid) ? aid : null;
            var template = new ProjectTemplate
            {
                PublicId = "TPL-" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper(),
                Name = request.TemplateName,
                Description = project.Description,
                BillingType = project.BillingModel,
                IsPublic = true,
                SourceProjectId = project.Id,
                CreatedById = createdById,
            };

            // `include_milestones` gates whether milestone-scoped tasks are carried over —
            // TemplateTask has no milestone concept of its own (source doesn't model one
            // either), so this is the closest faithful interpretation of the flag rather
            // than inventing a new field to track it.
            var sourceTasks = project.Tasks
                .Where(t => request.IncludeMilestones || t.MilestoneId == null)
                .OrderBy(t => t.Id);

            var order = 0;
            foreach (var task in sourceTasks)
            {
                template.Tasks.Add(new TemplateTask
                {
                    Title = task.TaskName,
                    Description = task.Description,
                    EstimatedHours = task.EstimatedHours,
                    Duration = task.Duration,
                    BillingType = task.BillingType,
                    Tags = task.Tags,
                    OrderIndex = order++,
                });
            }

            _context.ProjectTemplates.Add(template);
            await _context.SaveChangesAsync();

            return new ProjectTemplateResponse(
                template.Id, template.PublicId, template.Name, template.Description,
                template.BillingType, template.IsPublic, template.SourceProjectId, template.CreatedById,
                template.Tasks.Select(t => new TemplateTaskResponse(
                    t.Id, t.TemplateId, t.Title, t.Description, t.EstimatedHours, t.Duration, t.BillingType, t.Tags, t.OrderIndex)),
                template.CreatedAt, template.UpdatedAt
            );
        }, nameof(CloneToTemplateAsync));
    }

    private static MasterLookupResponse? MapMasterLookup(MasterLookup? lookup) => lookup == null
        ? null
        : new MasterLookupResponse(lookup.Id, lookup.Category, lookup.Value, lookup.Label, lookup.Color, lookup.Icon, lookup.OrderIndex, lookup.IsActive);

    private static UserResponseBaseDto? MapUserBase(User? user) => user == null
        ? null
        : new UserResponseBaseDto(user.Id, user.PublicId, user.Email, user.Username, user.FirstName, user.LastName, user.DisplayName, user.IsExternal, user.IsSynced);

    private static ProjectResponse MapToProjectResponse(Project p, int taskCount = 0, int issueCount = 0, int milestoneCount = 0) => new(
        p.Id,
        p.PublicId,
        p.ProjectIdSync,
        p.AccountName,
        p.ProjectName,
        p.CustomerName,
        p.ClientName,
        p.Tags,
        p.BillingModel,
        p.ProjectType,
        p.ProjectStatusExternal,
        p.ExpectedStartDate,
        p.ExpectedEndDate,
        p.Description,
        p.StatusId,
        p.PriorityId,
        MapMasterLookup(p.StatusMaster),
        MapMasterLookup(p.PriorityMaster),
        p.OwnerId,
        MapUserBase(p.Owner),
        p.ProjectManagerId,
        MapUserBase(p.ProjectManager),
        p.DeliveryHeadId,
        MapUserBase(p.DeliveryHead),
        p.EstimatedHours,
        p.ActualHours,
        p.ActualStartDate,
        p.ActualEndDate,
        p.IsArchived,
        p.IsTemplate,
        p.IsGroup,
        p.TeamMembers?.Count ?? 0,
        taskCount,
        issueCount,
        milestoneCount,
        p.TeamMembers?.Select(MapToProjectMemberResponse) ?? Enumerable.Empty<ProjectMemberResponseDto>(),
        p.CreatedAt,
        p.UpdatedAt
    );

    private static ProjectMemberResponseDto MapToProjectMemberResponse(ProjectMember m) => new(
        m.ProjectId,
        m.UserId,
        m.ProjectProfile,
        m.PortalProfile,
        m.RoleInProject,
        m.InvitationStatusId,
        m.IsOwner,
        m.CreatedAt,
        MapUserBase(m.User)
    );
}
