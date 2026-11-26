using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using PMS.Application.Common;
using PMS.Application.DTOs.Requests;
using PMS.Application.Services.Implementations;
using PMS.Domain.Entities;
using PMS.Infrastructure.Data;
using Task = System.Threading.Tasks.Task;
using TaskEntity = PMS.Domain.Entities.Task;

namespace PMS.Tests;

/// <summary>Sub-tasks (two levels), project links and project notes.</summary>
public class SubtasksLinksNotesTests
{
    private sealed class World : IDisposable
    {
        public required ApplicationDbContext Db { get; init; }
        public required Role EmployeeRole { get; init; }
        public required Role FreelancerRole { get; init; }
        public required User Admin { get; init; }
        public required User Author { get; init; }        // Employee
        public required User SameRole { get; init; }      // Employee
        public required User OtherRole { get; init; }     // Freelancer
        public required User SharedUser { get; init; }    // Freelancer, shared by name
        public required Project Project { get; init; }
        public required MasterLookup Completed { get; init; }
        public required MasterLookup Open { get; init; }
        public void Dispose() => Db.Dispose();

        public IEnumerable<(string Name, User User)> Everyone =>
            [("admin", Admin), ("author", Author), ("sameRole", SameRole), ("otherRole", OtherRole), ("sharedUser", SharedUser)];

        public async Task<AccessScope> ScopeAsync(User u) { var s = new AccessScope(Db); await s.LoadAsync(u.Id); return s; }
        public async Task<TaskService> TasksAsync(User u) => new(NullLogger<TaskService>.Instance, Db, await ScopeAsync(u));
        public async Task<ProjectLinkService> LinksAsync(User u) => new(NullLogger<ProjectLinkService>.Instance, Db, await ScopeAsync(u));
        public async Task<ProjectNoteService> NotesAsync(User u) => new(NullLogger<ProjectNoteService>.Instance, Db, await ScopeAsync(u));

        public async Task<HashSet<string>> WhoSeesLinkAsync(int id)
        {
            var seen = new HashSet<string>();
            foreach (var (name, user) in Everyone)
                if (await Db.ProjectLinks.ScopeLinks(await ScopeAsync(user)).AnyAsync(l => l.Id == id)) seen.Add(name);
            return seen;
        }

        public async Task<HashSet<string>> WhoSeesNoteAsync(int id)
        {
            var seen = new HashSet<string>();
            foreach (var (name, user) in Everyone)
                if (await Db.ProjectNotes.ScopeNotes(await ScopeAsync(user)).AnyAsync(n => n.Id == id)) seen.Add(name);
            return seen;
        }
    }

    private static async Task<World> BuildAsync()
    {
        var db = TestSupport.NewContext();
        var (admin, employee, freelancer, active, _) = await TestSupport.SeedRolesAsync(db);
        var a = TestSupport.NewUser("admin@x.com", admin.Id, active.Id);
        var author = TestSupport.NewUser("author@x.com", employee.Id, active.Id);
        var same = TestSupport.NewUser("same@x.com", employee.Id, active.Id);
        var other = TestSupport.NewUser("other@x.com", freelancer.Id, active.Id);
        var shared = TestSupport.NewUser("shared@x.com", freelancer.Id, active.Id);
        db.Users.AddRange(a, author, same, other, shared);
        var project = TestSupport.NewProject("P");
        db.Projects.Add(project);
        var completed = new MasterLookup { Category = "TaskStatus", Value = "completed", Label = "Completed" };
        var open = new MasterLookup { Category = "TaskStatus", Value = "open", Label = "Open" };
        db.MasterLookups.AddRange(completed, open);
        await db.SaveChangesAsync();
        // Freelancers are scoped to projects they belong to; sharing never widens project access.
        foreach (var u in new[] { author, same, other, shared })
            db.ProjectMembers.Add(new ProjectMember { ProjectId = project.Id, UserId = u.Id });
        await db.SaveChangesAsync();
        return new World
        {
            Db = db, EmployeeRole = employee, FreelancerRole = freelancer, Admin = a, Author = author,
            SameRole = same, OtherRole = other, SharedUser = shared, Project = project, Completed = completed, Open = open,
        };
    }

    // ================================================================ sub-tasks

    private static CreateTaskRequest NewTask(World w, string name, int? parent = null, int? statusId = null, int? listId = null, int? milestoneId = null) =>
        new(name, ProjectId: w.Project.Id, TaskListId: listId, MilestoneId: milestoneId, StatusId: statusId, ParentTaskId: parent);

    [Fact]
    public async Task Subtask_inherits_project_list_and_milestone_from_its_parent()
    {
        using var w = await BuildAsync();
        var svc = await w.TasksAsync(w.Admin);
        var parent = await svc.CreateTaskAsync(NewTask(w, "Parent", listId: 11, milestoneId: 22), w.Admin.Id.ToString());

        // The request tries to put the sub-task somewhere else: the parent wins.
        var child = await svc.CreateTaskAsync(new("Child", ProjectId: 9999, TaskListId: 33, MilestoneId: 44, ParentTaskId: parent.Id), w.Admin.Id.ToString());

        Assert.Equal(parent.Id, child.ParentTaskId);
        Assert.Equal(w.Project.Id, child.ProjectId);
        Assert.Equal(11, child.TaskListId);
        Assert.Equal(22, child.MilestoneId);
    }

    [Fact]
    public async Task A_subtask_cannot_have_subtasks_of_its_own()
    {
        using var w = await BuildAsync();
        var svc = await w.TasksAsync(w.Admin);
        var parent = await svc.CreateTaskAsync(NewTask(w, "Parent"), "1");
        var child = await svc.CreateTaskAsync(NewTask(w, "Child", parent.Id), "1");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => svc.CreateTaskAsync(NewTask(w, "Grandchild", child.Id), "1"));
        Assert.Contains("2 levels", ex.Message);
        Assert.Equal(2, await w.Db.Tasks.CountAsync());
    }

    [Fact]
    public async Task Subtask_of_a_missing_parent_is_rejected()
    {
        using var w = await BuildAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await (await w.TasksAsync(w.Admin)).CreateTaskAsync(NewTask(w, "Orphan", parent: 4242), "1"));
    }

    [Fact]
    public async Task Parent_reports_subtask_counts_done_count_and_hours()
    {
        using var w = await BuildAsync();
        var svc = await w.TasksAsync(w.Admin);
        var parent = await svc.CreateTaskAsync(NewTask(w, "Parent"), "1");
        var a = await svc.CreateTaskAsync(NewTask(w, "A", parent.Id, w.Completed.Id), "1");
        await svc.CreateTaskAsync(NewTask(w, "B", parent.Id, w.Open.Id), "1");
        await svc.CreateTaskAsync(NewTask(w, "C", parent.Id, w.Completed.Id), "1");
        foreach (var t in w.Db.Tasks.Where(t => t.ParentTaskId == parent.Id)) t.CachedTimelogTotal = 1.5m;
        await w.Db.SaveChangesAsync();

        var fetched = (await svc.GetTaskByIdAsync(parent.Id))!;
        Assert.Equal((3, 2, 4.5m), (fetched.SubtaskCount, fetched.SubtasksDone, fetched.SubtasksHours));
        Assert.Equal(0, (await svc.GetTaskByIdAsync(a.Id))!.SubtaskCount);
    }

    [Fact]
    public async Task Listing_can_return_only_top_level_tasks_or_only_one_parents_children()
    {
        using var w = await BuildAsync();
        var svc = await w.TasksAsync(w.Admin);
        var p1 = await svc.CreateTaskAsync(NewTask(w, "P1"), "1");
        var p2 = await svc.CreateTaskAsync(NewTask(w, "P2"), "1");
        var c1 = await svc.CreateTaskAsync(NewTask(w, "C1", p1.Id), "1");
        var c2 = await svc.CreateTaskAsync(NewTask(w, "C2", p1.Id), "1");
        await svc.CreateTaskAsync(NewTask(w, "C3", p2.Id), "1");

        var top = await svc.GetTasksAsync(0, 50, w.Project.Id, null, null, null, topLevelOnly: true);
        Assert.Equal(new HashSet<int> { p1.Id, p2.Id }, top.Items.Select(t => t.Id).ToHashSet());

        var kids = await svc.GetTasksAsync(0, 50, w.Project.Id, null, null, null, parentTaskId: p1.Id);
        Assert.Equal(new HashSet<int> { c1.Id, c2.Id }, kids.Items.Select(t => t.Id).ToHashSet());

        var all = await svc.GetTasksAsync(0, 50, w.Project.Id, null, null, null);
        Assert.Equal(5, all.Total);
    }

    [Fact]
    public async Task Moving_a_parent_moves_its_subtasks_and_a_subtask_cannot_move_alone()
    {
        using var w = await BuildAsync();
        var svc = await w.TasksAsync(w.Admin);
        var parent = await svc.CreateTaskAsync(NewTask(w, "Parent", listId: 1, milestoneId: 1), "1");
        var child = await svc.CreateTaskAsync(NewTask(w, "Child", parent.Id), "1");

        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.UpdateTaskAsync(child.Id, new TaskUpdateRequest(TaskListId: 2), "1"));

        await svc.UpdateTaskAsync(parent.Id, new TaskUpdateRequest(TaskListId: 2, MilestoneId: 3), "1");
        var moved = (await svc.GetTaskByIdAsync(child.Id))!;
        Assert.Equal((2, 3), (moved.TaskListId, moved.MilestoneId));
    }

    [Fact]
    public async Task Deleting_a_parent_deletes_its_subtasks_but_deleting_a_subtask_leaves_the_parent()
    {
        using var w = await BuildAsync();
        var svc = await w.TasksAsync(w.Admin);
        var p1 = await svc.CreateTaskAsync(NewTask(w, "P1"), "1");
        var c1 = await svc.CreateTaskAsync(NewTask(w, "C1", p1.Id), "1");
        var c2 = await svc.CreateTaskAsync(NewTask(w, "C2", p1.Id), "1");
        var p2 = await svc.CreateTaskAsync(NewTask(w, "P2"), "1");
        var c3 = await svc.CreateTaskAsync(NewTask(w, "C3", p2.Id), "1");

        Assert.True(await svc.DeleteTaskAsync(c3.Id, "1"));
        Assert.NotNull(await svc.GetTaskByIdAsync(p2.Id));

        Assert.True(await svc.DeleteTaskAsync(p1.Id, "1"));
        Assert.Null(await svc.GetTaskByIdAsync(c1.Id));
        Assert.Null(await svc.GetTaskByIdAsync(c2.Id));
        Assert.Equal(new HashSet<int> { p2.Id }, (await w.Db.Tasks.ToListAsync()).Select(t => t.Id).ToHashSet());
    }

    // ================================================================ links

    private static CreateProjectLinkRequest Link(string url = "https://github.com/acme/app", string type = "Repo", bool restrict = false,
        int[]? roles = null, int[]? users = null) => new("Source", url, type, null, restrict, roles, users);

    [Theory]
    [InlineData("https://github.com/acme/app")]
    [InlineData("http://projects.techspear.in/path?x=1#y")]
    public async Task Link_accepts_http_and_https_urls(string url)
    {
        using var w = await BuildAsync();
        var created = await (await w.LinksAsync(w.Admin)).CreateLinkAsync(w.Project.Id, Link(url));
        Assert.Equal(url, created.Url);
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:text/html,<script>1</script>")]
    [InlineData("ftp://files.example.com/a")]
    [InlineData("example.com")]
    [InlineData("//example.com")]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Link_rejects_anything_that_is_not_a_full_http_or_https_address(string url)
    {
        using var w = await BuildAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await (await w.LinksAsync(w.Admin)).CreateLinkAsync(w.Project.Id, Link(url)));
        Assert.Empty(w.Db.ProjectLinks);
    }

    [Fact]
    public async Task Link_type_must_be_one_of_the_known_types()
    {
        using var w = await BuildAsync();
        var svc = await w.LinksAsync(w.Admin);
        foreach (var t in PMS.Domain.Constants.AppConstants.LinkTypes)
            await svc.CreateLinkAsync(w.Project.Id, Link(type: t));
        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.CreateLinkAsync(w.Project.Id, Link(type: "Bogus")));
    }

    [Fact]
    public async Task Adding_a_link_needs_edit_permission()
    {
        using var w = await BuildAsync();
        await Assert.ThrowsAsync<AccessDeniedException>(async () =>
            await (await w.LinksAsync(w.Author)).CreateLinkAsync(w.Project.Id, Link()));
    }

    [Fact]
    public async Task Link_sharing_enumerates_exactly_the_allowed_users()
    {
        using var w = await BuildAsync();
        var admin = await w.LinksAsync(w.Admin);
        var open = await admin.CreateLinkAsync(w.Project.Id, Link());
        var priv = await admin.CreateLinkAsync(w.Project.Id, Link(restrict: true));
        var byRole = await admin.CreateLinkAsync(w.Project.Id, Link(restrict: true, roles: [w.FreelancerRole.Id]));
        var byUser = await admin.CreateLinkAsync(w.Project.Id, Link(restrict: true, users: [w.SameRole.Id]));

        Assert.Equal(new HashSet<string> { "admin", "author", "sameRole", "otherRole", "sharedUser" }, await w.WhoSeesLinkAsync(open.Id));
        Assert.Equal(new HashSet<string> { "admin" }, await w.WhoSeesLinkAsync(priv.Id));
        Assert.Equal(new HashSet<string> { "admin", "otherRole", "sharedUser" }, await w.WhoSeesLinkAsync(byRole.Id));
        Assert.Equal(new HashSet<string> { "admin", "sameRole" }, await w.WhoSeesLinkAsync(byUser.Id));
    }

    [Fact]
    public async Task Only_the_creator_or_an_admin_can_change_or_delete_a_link()
    {
        using var w = await BuildAsync();
        var created = await (await w.LinksAsync(w.Admin)).CreateLinkAsync(w.Project.Id, Link());

        await Assert.ThrowsAsync<AccessDeniedException>(async () =>
            await (await w.LinksAsync(w.SameRole)).UpdateLinkAsync(w.Project.Id, created.Id, new UpdateProjectLinkRequest(Title: "x")));
        await Assert.ThrowsAsync<AccessDeniedException>(async () =>
            await (await w.LinksAsync(w.SameRole)).DeleteLinkAsync(w.Project.Id, created.Id));
        Assert.True(await (await w.LinksAsync(w.Admin)).DeleteLinkAsync(w.Project.Id, created.Id));
    }

    // ================================================================ notes

    private static CreateProjectNoteRequest Note(string body = "Deploy on Fridays only", bool restrict = false, int[]? roles = null, int[]? users = null) =>
        new(body, "Release rule", false, restrict, roles, users);

    [Fact]
    public async Task Any_project_member_can_add_a_note_and_pinned_notes_sort_first()
    {
        using var w = await BuildAsync();
        var svc = await w.NotesAsync(w.Author);
        await svc.CreateNoteAsync(w.Project.Id, Note("older"));
        var pinned = await svc.CreateNoteAsync(w.Project.Id, Note("pinned") with { IsPinned = true });
        await svc.CreateNoteAsync(w.Project.Id, Note("newest"));

        var list = await svc.GetNotesAsync(w.Project.Id);
        Assert.Equal(pinned.Id, list[0].Id);
        Assert.Equal(3, list.Count);
        Assert.Equal("author", list[0].AuthorName);
    }

    [Fact]
    public async Task Only_the_author_or_an_admin_can_edit_or_delete_a_note()
    {
        using var w = await BuildAsync();
        var note = await (await w.NotesAsync(w.Author)).CreateNoteAsync(w.Project.Id, Note());

        await Assert.ThrowsAsync<AccessDeniedException>(async () =>
            await (await w.NotesAsync(w.SameRole)).UpdateNoteAsync(w.Project.Id, note.Id, new UpdateProjectNoteRequest(Body: "hijack")));
        await Assert.ThrowsAsync<AccessDeniedException>(async () =>
            await (await w.NotesAsync(w.SameRole)).DeleteNoteAsync(w.Project.Id, note.Id));

        var edited = await (await w.NotesAsync(w.Author)).UpdateNoteAsync(w.Project.Id, note.Id, new UpdateProjectNoteRequest(Body: "mine"));
        Assert.Equal("mine", edited!.Body);
        Assert.True(await (await w.NotesAsync(w.Admin)).DeleteNoteAsync(w.Project.Id, note.Id));
    }

    [Fact]
    public async Task Note_sharing_enumerates_exactly_the_allowed_users()
    {
        using var w = await BuildAsync();
        var author = await w.NotesAsync(w.Author);
        var open = await author.CreateNoteAsync(w.Project.Id, Note());
        var priv = await author.CreateNoteAsync(w.Project.Id, Note(restrict: true));
        var byRole = await author.CreateNoteAsync(w.Project.Id, Note(restrict: true, roles: [w.FreelancerRole.Id]));
        var byUser = await author.CreateNoteAsync(w.Project.Id, Note(restrict: true, users: [w.SharedUser.Id]));

        Assert.Equal(new HashSet<string> { "admin", "author", "sameRole", "otherRole", "sharedUser" }, await w.WhoSeesNoteAsync(open.Id));
        Assert.Equal(new HashSet<string> { "admin", "author" }, await w.WhoSeesNoteAsync(priv.Id));
        Assert.Equal(new HashSet<string> { "admin", "author", "otherRole", "sharedUser" }, await w.WhoSeesNoteAsync(byRole.Id));
        Assert.Equal(new HashSet<string> { "admin", "author", "sharedUser" }, await w.WhoSeesNoteAsync(byUser.Id));
    }

    [Fact]
    public async Task Notes_reject_empty_and_oversized_bodies_and_unknown_share_targets()
    {
        using var w = await BuildAsync();
        var svc = await w.NotesAsync(w.Author);
        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.CreateNoteAsync(w.Project.Id, Note("   ")));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.CreateNoteAsync(w.Project.Id, Note(new string('x', PMS.Domain.Constants.AppConstants.NoteMaxBodyLength + 1))));
        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.CreateNoteAsync(w.Project.Id, Note(restrict: true, roles: [9999])));
        Assert.Empty(w.Db.ProjectNotes);
    }
}
