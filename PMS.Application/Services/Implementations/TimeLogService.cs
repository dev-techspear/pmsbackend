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

public class TimeLogService : BaseService<TimeLogService>, ITimeLogService
{
    private readonly IApplicationDbContext _context;
    private readonly IAccessScope _scope;

    public TimeLogService(ILogger<TimeLogService> logger, IApplicationDbContext context, IAccessScope scope) : base(logger)
    {
        _context = context;
        _scope = scope;
    }

    private IQueryable<TimeLog> IncludeGraph(IQueryable<TimeLog> query, bool scoped = true) =>
        (scoped ? query.ScopeTimeLogs(_scope) : query)
        .Include(tl => tl.User)
        .Include(tl => tl.ApprovalStatusMaster);

    // Ported from the source's SQLAlchemy after_insert/after_update/after_delete event
    // listener on TimeLog (see SOURCE_DATABASE_SCHEMA.md, `timelogs` section) — every
    // write that touches a task-scoped log must keep Task.CachedTimelogTotal in sync.
    // EF Core has no declarative equivalent, so it's done explicitly here rather than
    // left out, which the source's own history treats as a correctness requirement, not
    // an optimization.
    private async Task RecalculateTaskCachedTotalAsync(int taskId)
    {
        var total = await _context.Timelogs
            .Where(tl => tl.TaskId == taskId)
            .SumAsync(tl => (decimal?)tl.DailyLogHours) ?? 0m;

        var task = await _context.Tasks.FirstOrDefaultAsync(t => t.Id == taskId);
        if (task != null)
        {
            task.CachedTimelogTotal = total;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<TimeLogResponse> CreateTimeLogAsync(CreateTimeLogRequest request, string actorId)
    {
        return await ExecuteAsync(async () =>
        {
            await _context.EnsureProjectAccessAsync(_scope, request.ProjectId);
            int? createdById = int.TryParse(actorId, out var aid) ? aid : null;
            var publicId = "TL-" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();

            var timeLog = new TimeLog
            {
                PublicId = publicId,
                LogTitle = request.LogTitle,
                UserId = request.UserId,
                CreatedById = createdById,
                ProjectId = request.ProjectId,
                TaskId = request.TaskId,
                IssueId = request.IssueId,
                TimesheetId = request.TimesheetId,
                Date = request.Date,
                DailyLogHours = request.DailyLogHours,
                TimePeriod = request.TimePeriod,
                Notes = request.Notes,
                BillingType = request.BillingType,
                GeneralLog = request.GeneralLog,
            };
            _context.Timelogs.Add(timeLog);
            await _context.SaveChangesAsync();

            if (timeLog.TaskId.HasValue)
            {
                await RecalculateTaskCachedTotalAsync(timeLog.TaskId.Value);
            }

            var reloaded = await IncludeGraph(_context.Timelogs).FirstAsync(tl => tl.Id == timeLog.Id);
            return MapToResponse(reloaded);
        }, nameof(CreateTimeLogAsync));
    }

    public async Task<TimeLogPagedResponseDto> GetTimeLogsAsync(int skip, int limit, int? projectId, int? taskId, int? userId, int? issueId)
    {
        return await ExecuteAsync(async () =>
        {
            var queryable = IncludeGraph(_context.Timelogs).AsQueryable();
            if (projectId.HasValue) queryable = queryable.Where(tl => tl.ProjectId == projectId);
            if (taskId.HasValue) queryable = queryable.Where(tl => tl.TaskId == taskId);
            if (userId.HasValue) queryable = queryable.Where(tl => tl.UserId == userId);
            if (issueId.HasValue) queryable = queryable.Where(tl => tl.IssueId == issueId);

            var total = await queryable.CountAsync();
            var items = await queryable.OrderByDescending(tl => tl.Date).Skip(skip).Take(limit).ToListAsync();
            return new TimeLogPagedResponseDto(total, items.Select(MapToResponse));
        }, nameof(GetTimeLogsAsync));
    }

    public async Task<TimeLogResponse?> GetTimeLogByIdAsync(int timeLogId)
    {
        return await ExecuteAsync(async () =>
        {
            var timeLog = await IncludeGraph(_context.Timelogs).FirstOrDefaultAsync(tl => tl.Id == timeLogId);
            return timeLog == null ? null : MapToResponse(timeLog);
        }, nameof(GetTimeLogByIdAsync));
    }

    public async Task<TimeLogResponse?> UpdateTimeLogAsync(int timeLogId, TimeLogUpdateRequest request, string actorId)
    {
        return await ExecuteAsync(async () =>
        {
            var timeLog = await IncludeGraph(_context.Timelogs).FirstOrDefaultAsync(tl => tl.Id == timeLogId);
            if (timeLog == null) return null;

            if (request.Date.HasValue) timeLog.Date = request.Date.Value;
            if (request.DailyLogHours.HasValue) timeLog.DailyLogHours = request.DailyLogHours.Value;
            if (request.LogTitle != null) timeLog.LogTitle = request.LogTitle;
            if (request.TimePeriod != null) timeLog.TimePeriod = request.TimePeriod;
            if (request.Notes != null) timeLog.Notes = request.Notes;
            if (request.BillingType != null) timeLog.BillingType = request.BillingType;
            if (request.ApprovalStatusId.HasValue)
            {
                timeLog.PreviousApprovalStatusId = timeLog.ApprovalStatusId;
                timeLog.ApprovalStatusId = request.ApprovalStatusId;
            }

            await _context.SaveChangesAsync();

            if (timeLog.TaskId.HasValue)
            {
                await RecalculateTaskCachedTotalAsync(timeLog.TaskId.Value);
            }

            return MapToResponse(timeLog);
        }, nameof(UpdateTimeLogAsync));
    }

    public async Task<bool> DeleteTimeLogAsync(int timeLogId, string actorId)
    {
        return await ExecuteAsync(async () =>
        {
            var timeLog = await _context.Timelogs.ScopeTimeLogs(_scope).FirstOrDefaultAsync(tl => tl.Id == timeLogId);
            if (timeLog == null) return false;

            var taskId = timeLog.TaskId;
            timeLog.IsDeleted = true;
            await _context.SaveChangesAsync();

            if (taskId.HasValue)
            {
                await RecalculateTaskCachedTotalAsync(taskId.Value);
            }
            return true;
        }, nameof(DeleteTimeLogAsync));
    }

    private static MasterLookupResponse? MapMasterLookup(MasterLookup? lookup) => lookup == null
        ? null
        : new MasterLookupResponse(lookup.Id, lookup.Category, lookup.Value, lookup.Label, lookup.Color, lookup.Icon, lookup.OrderIndex, lookup.IsActive);

    private static UserResponseBaseDto? MapUserBase(User? user) => user == null
        ? null
        : new UserResponseBaseDto(user.Id, user.PublicId, user.Email, user.Username, user.FirstName, user.LastName, user.DisplayName, user.IsExternal, user.IsSynced);

    private static TimeLogResponse MapToResponse(TimeLog tl) => new(
        tl.Id, tl.PublicId, tl.LogTitle, tl.UserId, MapUserBase(tl.User),
        tl.ProjectId, tl.TaskId, tl.IssueId, tl.TimesheetId,
        tl.Date, tl.DailyLogHours, tl.TimePeriod, tl.Notes, tl.BillingType,
        tl.ApprovalStatusId, MapMasterLookup(tl.ApprovalStatusMaster), tl.GeneralLog,
        tl.CreatedAt, tl.UpdatedAt
    );
}
