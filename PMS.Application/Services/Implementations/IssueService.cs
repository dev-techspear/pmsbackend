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

public class IssueService : BaseService<IssueService>, IIssueService
{
    private readonly IApplicationDbContext _context;
    private readonly IAccessScope _scope;

    public IssueService(ILogger<IssueService> logger, IApplicationDbContext context, IAccessScope scope) : base(logger)
    {
        _context = context;
        _scope = scope;
    }

    private IQueryable<Issue> IncludeGraph(IQueryable<Issue> query, bool scoped = true) =>
        (scoped ? query.ScopeIssues(_scope) : query)
        .Include(i => i.Reporter)
        .Include(i => i.Assignee)
        .Include(i => i.StatusMaster)
        .Include(i => i.PriorityMaster)
        .Include(i => i.SeverityMaster)
        .Include(i => i.ClassificationMaster)
        .Include(i => i.Followers)
        .Include(i => i.Assignees);

    public async Task<IssueResponse> CreateIssueAsync(CreateIssueRequest request, string actorId)
    {
        return await ExecuteAsync(async () =>
        {
            await _context.EnsureProjectAccessAsync(_scope, request.ProjectId);
            var publicId = "BUG-" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();
            var issue = new Issue
            {
                PublicId = publicId,
                BugName = request.BugName,
                ProjectId = request.ProjectId,
                MilestoneId = request.MilestoneId,
                AssociatedTeamId = request.AssociatedTeamId,
                ReporterId = request.ReporterId,
                AssigneeId = request.AssigneeId,
                StatusId = request.StatusId,
                PriorityId = request.PriorityId,
                SeverityId = request.SeverityId,
                ClassificationId = request.ClassificationId,
                Description = request.Description,
                Module = request.Module,
                Tags = request.Tags,
                Flag = request.Flag,
                ReproducibleFlag = request.ReproducibleFlag,
                StartDate = request.StartDate,
                DueDate = request.DueDate,
                EstimatedHours = request.EstimatedHours,
            };

            if (request.FollowerIds != null && request.FollowerIds.Any())
            {
                var followers = await _context.Users.Where(u => request.FollowerIds.Contains(u.Id)).ToListAsync();
                foreach (var f in followers) issue.Followers.Add(f);
            }
            if (request.AssigneeIds != null && request.AssigneeIds.Any())
            {
                var assignees = await _context.Users.Where(u => request.AssigneeIds.Contains(u.Id)).ToListAsync();
                foreach (var a in assignees) issue.Assignees.Add(a);
            }

            _context.Issues.Add(issue);
            await _context.SaveChangesAsync();

            var reloaded = await IncludeGraph(_context.Issues, scoped: false).FirstAsync(i => i.Id == issue.Id);
            return MapToIssueResponse(reloaded);
        }, nameof(CreateIssueAsync));
    }

    public async Task<IssuePagedResponseDto> GetIssuesAsync(int skip, int limit, int? projectId, int? statusId, int? assigneeId, int? severityId)
    {
        return await ExecuteAsync(async () =>
        {
            var queryable = IncludeGraph(_context.Issues).AsQueryable();
            if (projectId.HasValue) queryable = queryable.Where(i => i.ProjectId == projectId);
            if (statusId.HasValue) queryable = queryable.Where(i => i.StatusId == statusId);
            if (assigneeId.HasValue) queryable = queryable.Where(i => i.AssigneeId == assigneeId);
            if (severityId.HasValue) queryable = queryable.Where(i => i.SeverityId == severityId);

            var total = await queryable.CountAsync();
            var items = await queryable.OrderByDescending(i => i.Id).Skip(skip).Take(limit).ToListAsync();
            return new IssuePagedResponseDto(total, items.Select(MapToIssueResponse));
        }, nameof(GetIssuesAsync));
    }

    public async Task<IEnumerable<IssueResponse>> SearchIssuesAsync(string query, int limit)
    {
        return await ExecuteAsync(async () =>
        {
            var queryable = IncludeGraph(_context.Issues).AsQueryable();
            if (!string.IsNullOrWhiteSpace(query))
            {
                var term = query.ToLower();
                queryable = queryable.Where(i => i.BugName.ToLower().Contains(term));
            }
            var items = await queryable.OrderBy(i => i.BugName).Take(limit).ToListAsync();
            return items.Select(MapToIssueResponse);
        }, nameof(SearchIssuesAsync));
    }

    public async Task<IssueResponse?> GetIssueByIdAsync(int issueId)
    {
        return await ExecuteAsync(async () =>
        {
            var issue = await IncludeGraph(_context.Issues).FirstOrDefaultAsync(i => i.Id == issueId);
            return issue == null ? null : MapToIssueResponse(issue);
        }, nameof(GetIssueByIdAsync));
    }

    public async Task<IssueResponse?> UpdateIssueAsync(int issueId, IssueUpdateRequest request, string actorId)
    {
        return await ExecuteAsync(async () =>
        {
            var issue = await IncludeGraph(_context.Issues).FirstOrDefaultAsync(i => i.Id == issueId);
            if (issue == null) return null;

            if (request.BugName != null) issue.BugName = request.BugName;
            if (request.MilestoneId.HasValue) issue.MilestoneId = request.MilestoneId;
            if (request.AssociatedTeamId.HasValue) issue.AssociatedTeamId = request.AssociatedTeamId;
            if (request.ReporterId.HasValue) issue.ReporterId = request.ReporterId;
            if (request.AssigneeId.HasValue) issue.AssigneeId = request.AssigneeId;
            if (request.StatusId.HasValue)
            {
                issue.PreviousStatusId = issue.StatusId;
                issue.StatusId = request.StatusId;
                if (request.StatusId != issue.PreviousStatusId) issue.LastModifiedTime = DateTime.UtcNow;
            }
            if (request.PriorityId.HasValue) issue.PriorityId = request.PriorityId;
            if (request.SeverityId.HasValue) issue.SeverityId = request.SeverityId;
            if (request.ClassificationId.HasValue) issue.ClassificationId = request.ClassificationId;
            if (request.Description != null) issue.Description = request.Description;
            if (request.Module != null) issue.Module = request.Module;
            if (request.Tags != null) issue.Tags = request.Tags;
            if (request.Flag != null) issue.Flag = request.Flag;
            if (request.ReproducibleFlag.HasValue) issue.ReproducibleFlag = request.ReproducibleFlag.Value;
            if (request.StartDate.HasValue) issue.StartDate = request.StartDate;
            if (request.DueDate.HasValue) issue.DueDate = request.DueDate;
            if (request.EstimatedHours.HasValue) issue.EstimatedHours = request.EstimatedHours;

            if (request.FollowerIds != null)
            {
                issue.Followers.Clear();
                var followers = await _context.Users.Where(u => request.FollowerIds.Contains(u.Id)).ToListAsync();
                foreach (var f in followers) issue.Followers.Add(f);
            }
            if (request.AssigneeIds != null)
            {
                issue.Assignees.Clear();
                var assignees = await _context.Users.Where(u => request.AssigneeIds.Contains(u.Id)).ToListAsync();
                foreach (var a in assignees) issue.Assignees.Add(a);
            }

            await _context.SaveChangesAsync();
            return MapToIssueResponse(issue);
        }, nameof(UpdateIssueAsync));
    }

    public async Task<bool> DeleteIssueAsync(int issueId, string actorId)
    {
        return await ExecuteAsync(async () =>
        {
            var issue = await _context.Issues.ScopeIssues(_scope).FirstOrDefaultAsync(i => i.Id == issueId);
            if (issue == null) return false;
            issue.IsDeleted = true;
            issue.LastClosedTime = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }, nameof(DeleteIssueAsync));
    }

    private static MasterLookupResponse? MapMasterLookup(MasterLookup? lookup) => lookup == null
        ? null
        : new MasterLookupResponse(lookup.Id, lookup.Category, lookup.Value, lookup.Label, lookup.Color, lookup.Icon, lookup.OrderIndex, lookup.IsActive);

    private static UserResponseBaseDto? MapUserBase(User? user) => user == null
        ? null
        : new UserResponseBaseDto(user.Id, user.PublicId, user.Email, user.Username, user.FirstName, user.LastName, user.DisplayName, user.IsExternal, user.IsSynced);

    private static IssueResponse MapToIssueResponse(Issue i) => new(
        i.Id, i.PublicId, i.BugName, i.Description,
        i.ProjectId, i.MilestoneId, i.AssociatedTeamId,
        i.ReporterId, MapUserBase(i.Reporter),
        i.AssigneeId, MapUserBase(i.Assignee),
        i.StatusId, i.PriorityId, i.SeverityId, i.ClassificationId,
        MapMasterLookup(i.StatusMaster), MapMasterLookup(i.PriorityMaster), MapMasterLookup(i.SeverityMaster), MapMasterLookup(i.ClassificationMaster),
        i.Module, i.Tags, i.Flag, i.ReproducibleFlag, i.StartDate, i.DueDate, i.EstimatedHours,
        i.Followers?.Select(MapUserBase).Where(x => x != null).Select(x => x!) ?? Enumerable.Empty<UserResponseBaseDto>(),
        i.Assignees?.Select(MapUserBase).Where(x => x != null).Select(x => x!) ?? Enumerable.Empty<UserResponseBaseDto>(),
        i.CreatedAt, i.UpdatedAt
    );
}
