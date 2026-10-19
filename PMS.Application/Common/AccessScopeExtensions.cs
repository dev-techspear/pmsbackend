using Microsoft.EntityFrameworkCore;
using PMS.Application.Common.Interfaces;
using PMS.Domain.Constants;
using PMS.Domain.Entities;
using TaskEntity = PMS.Domain.Entities.Task;

namespace PMS.Application.Common;

/// <summary>
/// Query filters that narrow each entity to what the caller may see.
/// Level "O" = records the user owns / is assigned; "A" = those plus everything in projects the
/// user is associated with (owner, manager, delivery head or member). Unrestricted callers get
/// the query back unchanged.
/// </summary>
public static class AccessScopeExtensions
{
    public static IQueryable<Project> ScopeProjects(this IQueryable<Project> q, IAccessScope s)
    {
        if (!s.IsRestricted(AppConstants.PermProjectView) || s.UserId is not int uid) return q;
        return s.Level(AppConstants.PermProjectView) == AppConstants.ViewLevelAssociated
            ? q.Where(p => p.OwnerId == uid || p.ProjectManagerId == uid || p.DeliveryHeadId == uid
                           || p.TeamMembers.Any(m => m.UserId == uid))
            : q.Where(p => p.OwnerId == uid || p.ProjectManagerId == uid);
    }

    public static IQueryable<TaskEntity> ScopeTasks(this IQueryable<TaskEntity> q, IAccessScope s)
    {
        if (!s.IsRestricted(AppConstants.PermTaskView) || s.UserId is not int uid) return q;
        return s.Level(AppConstants.PermTaskView) == AppConstants.ViewLevelAssociated
            ? q.Where(t => t.AssigneeId == uid || t.OwnerId == uid || t.CreatedById == uid
                           || t.Assignees.Any(u => u.Id == uid) || t.Owners.Any(u => u.Id == uid)
                           || (t.Project != null && (t.Project.OwnerId == uid || t.Project.ProjectManagerId == uid
                                || t.Project.DeliveryHeadId == uid || t.Project.TeamMembers.Any(m => m.UserId == uid))))
            : q.Where(t => t.AssigneeId == uid || t.OwnerId == uid || t.CreatedById == uid
                           || t.Assignees.Any(u => u.Id == uid) || t.Owners.Any(u => u.Id == uid));
    }

    public static IQueryable<TaskList> ScopeTaskLists(this IQueryable<TaskList> q, IAccessScope s)
    {
        if (!s.IsRestricted(AppConstants.PermTaskView) || s.UserId is not int uid) return q;
        return q.Where(tl => tl.Project.OwnerId == uid || tl.Project.ProjectManagerId == uid
                             || tl.Project.DeliveryHeadId == uid || tl.Project.TeamMembers.Any(m => m.UserId == uid));
    }

    public static IQueryable<Issue> ScopeIssues(this IQueryable<Issue> q, IAccessScope s)
    {
        if (!s.IsRestricted(AppConstants.PermIssueView) || s.UserId is not int uid) return q;
        return s.Level(AppConstants.PermIssueView) == AppConstants.ViewLevelAssociated
            ? q.Where(i => i.ReporterId == uid || i.AssigneeId == uid
                           || (i.Project != null && (i.Project.OwnerId == uid || i.Project.ProjectManagerId == uid
                                || i.Project.DeliveryHeadId == uid || i.Project.TeamMembers.Any(m => m.UserId == uid))))
            : q.Where(i => i.ReporterId == uid || i.AssigneeId == uid);
    }

    public static IQueryable<Milestone> ScopeMilestones(this IQueryable<Milestone> q, IAccessScope s)
    {
        if (!s.IsRestricted(AppConstants.PermMilestoneView) || s.UserId is not int uid) return q;
        return s.Level(AppConstants.PermMilestoneView) == AppConstants.ViewLevelAssociated
            ? q.Where(m => m.OwnerId == uid
                           || (m.Project != null && (m.Project.OwnerId == uid || m.Project.ProjectManagerId == uid
                                || m.Project.DeliveryHeadId == uid || m.Project.TeamMembers.Any(pm => pm.UserId == uid))))
            : q.Where(m => m.OwnerId == uid);
    }

    // Time entries are personal: both O and A see only the caller's own (matches the source app).
    public static IQueryable<TimeLog> ScopeTimeLogs(this IQueryable<TimeLog> q, IAccessScope s)
    {
        if (!s.IsRestricted(AppConstants.PermTimeView) || s.UserId is not int uid) return q;
        return q.Where(tl => tl.UserId == uid || tl.CreatedById == uid);
    }

    public static IQueryable<Timesheet> ScopeTimesheets(this IQueryable<Timesheet> q, IAccessScope s)
    {
        if (!s.IsRestricted(AppConstants.PermTimeView) || s.Email is not string email) return q;
        return q.Where(ts => ts.UserEmail == email);
    }

    public static IQueryable<Document> ScopeDocuments(this IQueryable<Document> q, IAccessScope s)
    {
        if (!s.IsRestricted(AppConstants.PermProjectView) || s.UserId is not int uid) return q;
        return q.Where(d => d.Project.OwnerId == uid || d.Project.ProjectManagerId == uid
                            || d.Project.DeliveryHeadId == uid || d.Project.TeamMembers.Any(m => m.UserId == uid));
    }

    /// <summary>
    /// Creating something inside a project requires that the caller can see that project.
    /// (Always evaluated for restricted callers; ProjectId null means "no project", which is allowed.)
    /// </summary>
    public static async System.Threading.Tasks.Task EnsureProjectAccessAsync(
        this IApplicationDbContext context, IAccessScope s, int? projectId)
    {
        if (projectId == null || !s.IsRestricted(AppConstants.PermProjectView)) return;
        var allowed = await context.Projects.ScopeProjects(s).AnyAsync(p => p.Id == projectId);
        if (!allowed) throw new AccessDeniedException("You do not have access to that project.");
    }
}
