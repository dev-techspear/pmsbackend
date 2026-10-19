using Microsoft.EntityFrameworkCore;
using PMS.API.Security;
using PMS.Application.Common;
using PMS.Domain.Constants;
using PMS.Domain.Entities;
using PMS.Infrastructure.Data;
using Task = System.Threading.Tasks.Task;
using TaskEntity = PMS.Domain.Entities.Task;

namespace PMS.Tests;

public class AccessScopeTests
{
    // ---- who is restricted ----

    private static async Task<AccessScope> ScopeFor(ApplicationDbContext db, User user)
    {
        var scope = new AccessScope(db);
        await scope.LoadAsync(user.Id);
        return scope;
    }

    [Fact]
    public async Task Admin_is_never_restricted()
    {
        using var db = TestSupport.NewContext();
        var (admin, _, _, active, _) = await TestSupport.SeedRolesAsync(db);
        var u = TestSupport.NewUser("a@x.com", admin.Id, active.Id);
        db.Users.Add(u); await db.SaveChangesAsync();

        var scope = await ScopeFor(db, u);
        Assert.False(scope.IsRestricted(AppConstants.PermProjectView));
        Assert.Equal(AppConstants.ViewLevelAll, scope.Level(AppConstants.PermProjectView));
    }

    [Fact]
    public async Task Internal_user_with_no_value_stays_unrestricted_but_O_and_A_restrict()
    {
        using var db = TestSupport.NewContext();
        var (_, employee, _, active, _) = await TestSupport.SeedRolesAsync(db);
        employee.Permissions = """{"proj-view":"O","task-view":"A","issue-view":"All","milestone-view":true}""";
        var u = TestSupport.NewUser("e@x.com", employee.Id, active.Id);
        db.Users.Add(u); await db.SaveChangesAsync();

        var scope = await ScopeFor(db, u);
        Assert.True(scope.IsRestricted(AppConstants.PermProjectView));    // O
        Assert.True(scope.IsRestricted(AppConstants.PermTaskView));       // A
        Assert.False(scope.IsRestricted(AppConstants.PermIssueView));     // All
        Assert.False(scope.IsRestricted(AppConstants.PermMilestoneView)); // true == All
        Assert.False(scope.IsRestricted(AppConstants.PermTimeView));      // not set, internal
    }

    [Fact]
    public async Task External_user_is_restricted_even_for_permissions_the_role_lacks()
    {
        using var db = TestSupport.NewContext();
        var (_, _, freelancer, active, _) = await TestSupport.SeedRolesAsync(db);
        var u = TestSupport.NewUser("f@x.com", freelancer.Id, active.Id, external: true);
        db.Users.Add(u); await db.SaveChangesAsync();

        var scope = await ScopeFor(db, u);
        Assert.True(scope.IsRestricted("proj-delete"));
        Assert.Null(scope.Level("proj-delete"));
    }

    [Fact]
    public async Task Malformed_permission_json_grants_nothing()
    {
        using var db = TestSupport.NewContext();
        var (_, _, freelancer, active, _) = await TestSupport.SeedRolesAsync(db, "{ not json");
        var u = TestSupport.NewUser("f@x.com", freelancer.Id, active.Id, external: true);
        db.Users.Add(u); await db.SaveChangesAsync();

        var scope = await ScopeFor(db, u);
        Assert.Null(scope.Level(AppConstants.PermProjectView));
    }

    // ---- what a restricted user sees ----

    private sealed record World(ApplicationDbContext Db, AccessScope Freelancer, Project Mine, Project Other,
        TaskEntity MyTask, TaskEntity OtherTask, TimeLog MyLog, TimeLog OtherLog) : IDisposable
    {
        public void Dispose() => Db.Dispose();
    }

    private static async Task<World> BuildWorldAsync(string permissions)
    {
        var db = TestSupport.NewContext();
        var (admin, employee, freelancer, active, _) = await TestSupport.SeedRolesAsync(db, permissions);
        var boss = TestSupport.NewUser("boss@x.com", employee.Id, active.Id);
        var free = TestSupport.NewUser("free@x.com", freelancer.Id, active.Id, external: true);
        db.Users.AddRange(boss, free);
        await db.SaveChangesAsync();

        var mine = TestSupport.NewProject("Mine", ownerId: boss.Id);
        mine.TeamMembers.Add(new ProjectMember { UserId = free.Id });
        var other = TestSupport.NewProject("Other", ownerId: boss.Id);
        db.Projects.AddRange(mine, other);
        await db.SaveChangesAsync();

        var myTask = TestSupport.NewTask("in-mine", mine.Id);
        var otherTask = TestSupport.NewTask("in-other", other.Id);
        db.Tasks.AddRange(myTask, otherTask);
        var myLog = new TimeLog { UserId = free.Id, ProjectId = mine.Id };
        var otherLog = new TimeLog { UserId = boss.Id, ProjectId = mine.Id };
        db.Timelogs.AddRange(myLog, otherLog);
        await db.SaveChangesAsync();

        var scope = new AccessScope(db);
        await scope.LoadAsync(free.Id);
        return new World(db, scope, mine, other, myTask, otherTask, myLog, otherLog);
    }

    [Fact]
    public async Task Associated_level_shows_only_projects_the_user_is_a_member_of()
    {
        using var w = await BuildWorldAsync(TestSupport.FreelancerPermissionsAssociated);
        var names = await w.Db.Projects.ScopeProjects(w.Freelancer).Select(p => p.ProjectName).ToListAsync();
        Assert.Equal(new[] { "Mine" }, names);
    }

    [Fact]
    public async Task Owned_level_hides_projects_where_the_user_is_only_a_member()
    {
        using var w = await BuildWorldAsync("""{"proj-view":"O"}""");
        Assert.Empty(await w.Db.Projects.ScopeProjects(w.Freelancer).ToListAsync());
    }

    [Fact]
    public async Task All_level_is_unfiltered()
    {
        using var w = await BuildWorldAsync("""{"proj-view":"All","task-view":"All"}""");
        Assert.Equal(2, await w.Db.Projects.ScopeProjects(w.Freelancer).CountAsync());
        Assert.Equal(2, await w.Db.Tasks.ScopeTasks(w.Freelancer).CountAsync());
    }

    [Fact]
    public async Task Associated_tasks_are_only_those_in_the_users_projects()
    {
        using var w = await BuildWorldAsync(TestSupport.FreelancerPermissionsAssociated);
        var names = await w.Db.Tasks.ScopeTasks(w.Freelancer).Select(t => t.TaskName).ToListAsync();
        Assert.Equal(new[] { "in-mine" }, names);
    }

    [Fact]
    public async Task Time_logs_are_personal_even_inside_a_shared_project()
    {
        using var w = await BuildWorldAsync(TestSupport.FreelancerPermissionsAssociated);
        var logs = await w.Db.Timelogs.ScopeTimeLogs(w.Freelancer).ToListAsync();
        Assert.Equal(new[] { w.MyLog.Id }, logs.Select(l => l.Id));
    }

    [Fact]
    public async Task Creating_inside_a_project_the_user_cannot_see_is_denied()
    {
        using var w = await BuildWorldAsync(TestSupport.FreelancerPermissionsAssociated);
        await w.Db.EnsureProjectAccessAsync(w.Freelancer, w.Mine.Id);   // fine
        await w.Db.EnsureProjectAccessAsync(w.Freelancer, null);        // no project, fine
        await Assert.ThrowsAsync<AccessDeniedException>(
            () => w.Db.EnsureProjectAccessAsync(w.Freelancer, w.Other.Id));
    }

    // ---- every scope must translate to real SQL Server SQL (InMemory would hide a bad expression) ----

    [Theory]
    [InlineData("O")]
    [InlineData("A")]
    public async Task Every_scope_translates_to_sql(string level)
    {
        var perms = $$"""{"proj-view":"{{level}}","task-view":"{{level}}","issue-view":"{{level}}","milestone-view":"{{level}}","time-view":"{{level}}"}""";
        using var mem = TestSupport.NewContext();
        var (_, _, freelancer, active, _) = await TestSupport.SeedRolesAsync(mem, perms);
        var u = TestSupport.NewUser("f@x.com", freelancer.Id, active.Id, external: true);
        mem.Users.Add(u); await mem.SaveChangesAsync();
        var scope = await ScopeFor(mem, u);

        using var sql = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=localhost;Database=never_connected;Trusted_Connection=True;").Options);

        var queries = new[]
        {
            sql.Projects.ScopeProjects(scope).ToQueryString(),
            sql.Tasks.ScopeTasks(scope).ToQueryString(),
            sql.TaskLists.ScopeTaskLists(scope).ToQueryString(),
            sql.Issues.ScopeIssues(scope).ToQueryString(),
            sql.Milestones.ScopeMilestones(scope).ToQueryString(),
            sql.Timelogs.ScopeTimeLogs(scope).ToQueryString(),
            sql.Timesheets.ScopeTimesheets(scope).ToQueryString(),
            sql.Documents.ScopeDocuments(scope).ToQueryString(),
        };
        Assert.All(queries, q => Assert.Contains("WHERE", q));   // a filter was actually applied
    }

    // ---- endpoint permission map for external users ----

    [Theory]
    [InlineData("Auth", "POST", "/api/v1/auth/login", ExternalPermissionMap.Allowed)]
    [InlineData("Masters", "GET", "/api/v1/masters/statuses", ExternalPermissionMap.Allowed)]
    [InlineData("Masters", "PUT", "/api/v1/masters/roles/bulk-permissions", null)]
    [InlineData("Users", "GET", "/api/v1/users/me", ExternalPermissionMap.Allowed)]
    [InlineData("Users", "GET", "/api/v1/users", null)]
    [InlineData("Users", "DELETE", "/api/v1/users/5", null)]
    [InlineData("Audit", "GET", "/api/v1/audit", null)]
    [InlineData("Projects", "GET", "/api/v1/projects", "proj-view")]
    [InlineData("Projects", "POST", "/api/v1/projects", "proj-create")]
    [InlineData("Projects", "PUT", "/api/v1/projects/3", "proj-edit")]
    [InlineData("Projects", "PATCH", "/api/v1/projects/3/archive", "proj-edit")]
    [InlineData("Projects", "DELETE", "/api/v1/projects/3", "proj-delete")]
    [InlineData("Projects", "POST", "/api/v1/projects/3/members", "proj-edit")]
    [InlineData("Projects", "DELETE", "/api/v1/projects/3/members/9", "proj-edit")]
    [InlineData("Documents", "POST", "/api/v1/documents", "proj-edit")]
    [InlineData("Tasks", "GET", "/api/v1/tasks", "task-view")]
    [InlineData("TaskLists", "POST", "/api/v1/tasklists", "task-create")]
    [InlineData("Issues", "DELETE", "/api/v1/issues/1", "issue-delete")]
    [InlineData("Milestones", "PUT", "/api/v1/milestones/1", "milestone-edit")]
    [InlineData("TimeLogs", "POST", "/api/v1/timelogs", "time-create")]
    [InlineData("Timesheets", "GET", "/api/v1/timesheets", "time-view")]
    [InlineData("Teams", "GET", "/api/v1/teams", "team-view")]
    [InlineData("Chat", "GET", "/api/v1/projects/3/chat/messages", "chat-view")]
    [InlineData("Chat", "GET", "/api/v1/chat/unread", "chat-view")]
    [InlineData("Chat", "GET", "/api/v1/projects/3/chat/members", "chat-view")]
    [InlineData("Chat", "POST", "/api/v1/projects/3/chat/read", "chat-view")]
    [InlineData("Chat", "POST", "/api/v1/projects/3/chat/messages", "chat-post")]
    [InlineData("Chat", "PUT", "/api/v1/projects/3/chat/messages/8", "chat-post")]
    [InlineData("Chat", "DELETE", "/api/v1/projects/3/chat/messages/8", "chat-post")]
    public void External_permission_map(string controller, string method, string path, string? expected)
    {
        Assert.Equal(expected, ExternalPermissionMap.RequiredPermission(controller, method, path));
    }

    [Fact]
    public void Every_api_controller_is_either_mapped_or_deliberately_closed_to_externals()
    {
        var controllers = typeof(AccessScopeFilter).Assembly.GetTypes()
            .Where(t => t is { IsAbstract: false } && t.Name.EndsWith("Controller") && t.Name != "BaseController")
            .Select(t => t.Name[..^"Controller".Length])
            .OrderBy(n => n).ToList();

        // Controllers external users are meant to reach. Anything else must stay out of the map.
        var reachable = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Auth", "Masters", "Users", "Projects", "ProjectGroups", "ProjectTemplates", "Documents",
            "Tasks", "TaskLists", "Issues", "Milestones", "TimeLogs", "Timesheets", "Teams", "Chat",
        };
        // Audit = system logs; Graph = searches the whole company directory (names/emails of every employee).
        var closed = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Audit", "Graph" };

        foreach (var c in controllers)
        {
            var open = ExternalPermissionMap.RequiredPermission(c, "GET", $"/api/v1/{c.ToLower()}") != null;
            if (reachable.Contains(c)) continue;
            Assert.True(closed.Contains(c) && !open,
                $"Controller '{c}' is not listed in this test: decide whether external users may reach it, " +
                "then add it to ExternalPermissionMap (and 'reachable') or to 'closed'.");
        }
    }
}
