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
using TaskEntity = PMS.Domain.Entities.Task;

namespace PMS.Application.Services.Implementations;

public class TaskService : BaseService<TaskService>, ITaskService
{
    private readonly IApplicationDbContext _context;
    private readonly IAccessScope _scope;

    public TaskService(ILogger<TaskService> logger, IApplicationDbContext context, IAccessScope scope) : base(logger)
    {
        _context = context;
        _scope = scope;
    }

    private IQueryable<TaskEntity> IncludeGraph(IQueryable<TaskEntity> query, bool scoped = true) =>
        (scoped ? query.ScopeTasks(_scope) : query)
        .Include(t => t.StatusMaster)
        .Include(t => t.PriorityMaster)
        .Include(t => t.Assignee)
        .Include(t => t.Owners)
        .Include(t => t.Assignees);

    public async Task<TaskResponse> CreateTaskAsync(CreateTaskRequest request, string actorId)
    {
        return await ExecuteAsync(async () =>
        {
            await _context.EnsureProjectAccessAsync(_scope, request.ProjectId);
            int? createdById = int.TryParse(actorId, out var aid) ? aid : null;
            var publicId = "TSK-" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();

            var task = new TaskEntity
            {
                PublicId = publicId,
                TaskName = request.TaskName,
                ProjectId = request.ProjectId,
                TaskListId = request.TaskListId,
                MilestoneId = request.MilestoneId,
                AssociatedTeamId = request.AssociatedTeamId,
                AssigneeId = request.AssigneeId,
                CreatedById = createdById,
                OwnerId = request.OwnerId,
                StatusId = request.StatusId,
                PriorityId = request.PriorityId,
                Description = request.Description,
                Tags = request.Tags,
                StartDate = request.StartDate,
                DueDate = request.DueDate,
                Duration = request.Duration,
                EstimatedHours = request.EstimatedHours,
                BillingType = request.BillingType,
            };

            if (request.OwnerIds != null && request.OwnerIds.Any())
            {
                var owners = await _context.Users.Where(u => request.OwnerIds.Contains(u.Id)).ToListAsync();
                foreach (var o in owners) task.Owners.Add(o);
            }
            if (request.AssigneeIds != null && request.AssigneeIds.Any())
            {
                var assignees = await _context.Users.Where(u => request.AssigneeIds.Contains(u.Id)).ToListAsync();
                foreach (var a in assignees) task.Assignees.Add(a);
            }

            _context.Tasks.Add(task);
            await _context.SaveChangesAsync();

            var reloaded = await IncludeGraph(_context.Tasks, scoped: false).FirstAsync(t => t.Id == task.Id);
            return MapToTaskResponse(reloaded);
        }, nameof(CreateTaskAsync));
    }

    public async Task<TaskPagedResponseDto> GetTasksAsync(int skip, int limit, int? projectId, int? taskListId, int? statusId, int? assigneeId)
    {
        return await ExecuteAsync(async () =>
        {
            var queryable = IncludeGraph(_context.Tasks).AsQueryable();

            if (projectId.HasValue) queryable = queryable.Where(t => t.ProjectId == projectId);
            if (taskListId.HasValue) queryable = queryable.Where(t => t.TaskListId == taskListId);
            if (statusId.HasValue) queryable = queryable.Where(t => t.StatusId == statusId);
            if (assigneeId.HasValue) queryable = queryable.Where(t => t.AssigneeId == assigneeId);

            var total = await queryable.CountAsync();
            var items = await queryable.OrderByDescending(t => t.Id).Skip(skip).Take(limit).ToListAsync();

            return new TaskPagedResponseDto(total, items.Select(MapToTaskResponse));
        }, nameof(GetTasksAsync));
    }

    public async Task<IEnumerable<TaskResponse>> SearchTasksAsync(string query, int limit)
    {
        return await ExecuteAsync(async () =>
        {
            var queryable = IncludeGraph(_context.Tasks).AsQueryable();
            if (!string.IsNullOrWhiteSpace(query))
            {
                var term = query.ToLower();
                queryable = queryable.Where(t => t.TaskName.ToLower().Contains(term));
            }
            var items = await queryable.OrderBy(t => t.TaskName).Take(limit).ToListAsync();
            return items.Select(MapToTaskResponse);
        }, nameof(SearchTasksAsync));
    }

    public async Task<TaskResponse?> GetTaskByIdAsync(int taskId)
    {
        return await ExecuteAsync(async () =>
        {
            var task = await IncludeGraph(_context.Tasks).FirstOrDefaultAsync(t => t.Id == taskId);
            return task == null ? null : MapToTaskResponse(task);
        }, nameof(GetTaskByIdAsync));
    }

    public async Task<TaskResponse?> UpdateTaskAsync(int taskId, TaskUpdateRequest request, string actorId)
    {
        return await ExecuteAsync(async () =>
        {
            var task = await IncludeGraph(_context.Tasks).FirstOrDefaultAsync(t => t.Id == taskId);
            if (task == null) return null;

            if (request.TaskName != null) task.TaskName = request.TaskName;
            if (request.TaskListId.HasValue) task.TaskListId = request.TaskListId;
            if (request.MilestoneId.HasValue) task.MilestoneId = request.MilestoneId;
            if (request.AssociatedTeamId.HasValue) task.AssociatedTeamId = request.AssociatedTeamId;
            if (request.AssigneeId.HasValue) task.AssigneeId = request.AssigneeId;
            if (request.OwnerId.HasValue) task.OwnerId = request.OwnerId;
            if (request.StatusId.HasValue)
            {
                task.PreviousStatusId = task.StatusId;
                task.StatusId = request.StatusId;
            }
            if (request.PriorityId.HasValue) task.PriorityId = request.PriorityId;
            if (request.Description != null) task.Description = request.Description;
            if (request.Tags != null) task.Tags = request.Tags;
            if (request.StartDate.HasValue) task.StartDate = request.StartDate;
            if (request.DueDate.HasValue) task.DueDate = request.DueDate;
            if (request.CompletionDate.HasValue) task.CompletionDate = request.CompletionDate;
            if (request.Duration.HasValue) task.Duration = request.Duration;
            if (request.CompletionPercentage.HasValue) task.CompletionPercentage = request.CompletionPercentage.Value;
            if (request.EstimatedHours.HasValue) task.EstimatedHours = request.EstimatedHours;
            if (request.WorkHours.HasValue) task.WorkHours = request.WorkHours;
            if (request.BillingType != null) task.BillingType = request.BillingType;

            if (request.OwnerIds != null)
            {
                task.Owners.Clear();
                var owners = await _context.Users.Where(u => request.OwnerIds.Contains(u.Id)).ToListAsync();
                foreach (var o in owners) task.Owners.Add(o);
            }
            if (request.AssigneeIds != null)
            {
                task.Assignees.Clear();
                var assignees = await _context.Users.Where(u => request.AssigneeIds.Contains(u.Id)).ToListAsync();
                foreach (var a in assignees) task.Assignees.Add(a);
            }

            await _context.SaveChangesAsync();
            return MapToTaskResponse(task);
        }, nameof(UpdateTaskAsync));
    }

    public async Task<bool> DeleteTaskAsync(int taskId, string actorId)
    {
        return await ExecuteAsync(async () =>
        {
            var task = await _context.Tasks.ScopeTasks(_scope).FirstOrDefaultAsync(t => t.Id == taskId);
            if (task == null) return false;
            task.IsDeleted = true;
            await _context.SaveChangesAsync();
            return true;
        }, nameof(DeleteTaskAsync));
    }

    private static MasterLookupResponse? MapMasterLookup(MasterLookup? lookup) => lookup == null
        ? null
        : new MasterLookupResponse(lookup.Id, lookup.Category, lookup.Value, lookup.Label, lookup.Color, lookup.Icon, lookup.OrderIndex, lookup.IsActive);

    private static UserResponseBaseDto? MapUserBase(User? user) => user == null
        ? null
        : new UserResponseBaseDto(user.Id, user.PublicId, user.Email, user.Username, user.FirstName, user.LastName, user.DisplayName, user.IsExternal, user.IsSynced);

    private static TaskResponse MapToTaskResponse(TaskEntity t) => new(
        t.Id, t.PublicId, t.TaskName, t.Description,
        t.ProjectId, t.TaskListId, t.MilestoneId, t.AssociatedTeamId,
        t.AssigneeId, MapUserBase(t.Assignee),
        t.CreatedById, t.OwnerId,
        t.StatusId, t.PriorityId, MapMasterLookup(t.StatusMaster), MapMasterLookup(t.PriorityMaster),
        t.Tags, t.StartDate, t.DueDate, t.CompletionDate, t.Duration, t.CompletionPercentage,
        t.EstimatedHours, t.WorkHours, t.CachedTimelogTotal, t.BillingType,
        t.Owners?.Select(MapUserBase).Where(x => x != null).Select(x => x!) ?? Enumerable.Empty<UserResponseBaseDto>(),
        t.Assignees?.Select(MapUserBase).Where(x => x != null).Select(x => x!) ?? Enumerable.Empty<UserResponseBaseDto>(),
        t.CreatedAt, t.UpdatedAt
    );
}

public class TaskListService : BaseService<TaskListService>, ITaskListService
{
    private readonly IApplicationDbContext _context;
    private readonly IAccessScope _scope;

    public TaskListService(ILogger<TaskListService> logger, IApplicationDbContext context, IAccessScope scope) : base(logger)
    {
        _context = context;
        _scope = scope;
    }

    public async Task<TaskListResponse> CreateTaskListAsync(CreateTaskListRequest request, string actorId)
    {
        return await ExecuteAsync(async () =>
        {
            await _context.EnsureProjectAccessAsync(_scope, request.ProjectId);
            var taskList = new PMS.Domain.Entities.TaskList
            {
                ProjectId = request.ProjectId,
                MilestoneId = request.MilestoneId,
                Name = request.Name,
                Description = request.Description,
            };
            _context.TaskLists.Add(taskList);
            await _context.SaveChangesAsync();
            return MapToTaskListResponse(taskList, 0);
        }, nameof(CreateTaskListAsync));
    }

    public async Task<IEnumerable<TaskListResponse>> GetTaskListsAsync(int? projectId)
    {
        return await ExecuteAsync(async () =>
        {
            var queryable = _context.TaskLists.ScopeTaskLists(_scope).Include(tl => tl.Tasks).AsQueryable();
            if (projectId.HasValue) queryable = queryable.Where(tl => tl.ProjectId == projectId);
            var items = await queryable.OrderBy(tl => tl.Id).ToListAsync();
            return items.Select(tl => MapToTaskListResponse(tl, tl.Tasks?.Count ?? 0));
        }, nameof(GetTaskListsAsync));
    }

    public async Task<TaskListResponse?> GetTaskListByIdAsync(int taskListId)
    {
        return await ExecuteAsync(async () =>
        {
            var taskList = await _context.TaskLists.ScopeTaskLists(_scope).Include(tl => tl.Tasks).FirstOrDefaultAsync(tl => tl.Id == taskListId);
            return taskList == null ? null : MapToTaskListResponse(taskList, taskList.Tasks?.Count ?? 0);
        }, nameof(GetTaskListByIdAsync));
    }

    public async Task<TaskListResponse?> UpdateTaskListAsync(int taskListId, TaskListUpdateRequest request, string actorId)
    {
        return await ExecuteAsync(async () =>
        {
            var taskList = await _context.TaskLists.ScopeTaskLists(_scope).Include(tl => tl.Tasks).FirstOrDefaultAsync(tl => tl.Id == taskListId);
            if (taskList == null) return null;

            if (request.Name != null) taskList.Name = request.Name;
            if (request.MilestoneId.HasValue) taskList.MilestoneId = request.MilestoneId;
            if (request.Description != null) taskList.Description = request.Description;

            await _context.SaveChangesAsync();
            return MapToTaskListResponse(taskList, taskList.Tasks?.Count ?? 0);
        }, nameof(UpdateTaskListAsync));
    }

    public async Task<bool> DeleteTaskListAsync(int taskListId, string actorId)
    {
        return await ExecuteAsync(async () =>
        {
            var taskList = await _context.TaskLists.ScopeTaskLists(_scope).FirstOrDefaultAsync(tl => tl.Id == taskListId);
            if (taskList == null) return false;
            taskList.IsDeleted = true;
            await _context.SaveChangesAsync();
            return true;
        }, nameof(DeleteTaskListAsync));
    }

    private static TaskListResponse MapToTaskListResponse(PMS.Domain.Entities.TaskList tl, int taskCount) => new(
        tl.Id, tl.ProjectId, tl.MilestoneId, tl.Name, tl.Description, taskCount, tl.CreatedAt, tl.UpdatedAt
    );
}
