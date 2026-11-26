using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using PMS.Application.Common;
using PMS.Application.DTOs.Requests;
using PMS.Application.Services.Implementations;
using PMS.Domain.Entities;
using PMS.Infrastructure.Data;
using Task = System.Threading.Tasks.Task;

namespace PMS.Tests;

/// <summary>
/// The create/edit forms name people by e-mail (owner_emails / assignee_emails). Those used to be
/// ignored, so no task was ever assigned. These tests pin the behaviour end to end.
/// </summary>
public class TaskAssignmentTests
{
    private sealed class World : IDisposable
    {
        public required ApplicationDbContext Db { get; init; }
        public required User Admin { get; init; }
        public required User Ravi { get; init; }
        public required User Meera { get; init; }
        public required Project Project { get; init; }
        public required TaskService Tasks { get; init; }
        public void Dispose() => Db.Dispose();
    }

    private static async Task<World> BuildAsync()
    {
        var db = TestSupport.NewContext();
        var (admin, employee, _, active, _) = await TestSupport.SeedRolesAsync(db);
        var a = TestSupport.NewUser("admin@x.com", admin.Id, active.Id);
        var ravi = TestSupport.NewUser("Ravi.Kumar@Techspear.dev", employee.Id, active.Id);   // mixed case on purpose
        var meera = TestSupport.NewUser("meera.nair@techspear.dev", employee.Id, active.Id);
        db.Users.AddRange(a, ravi, meera);
        var project = TestSupport.NewProject("P");
        db.Projects.Add(project);
        await db.SaveChangesAsync();
        var scope = new AccessScope(db);
        await scope.LoadAsync(a.Id);
        return new World
        {
            Db = db, Admin = a, Ravi = ravi, Meera = meera, Project = project,
            Tasks = new TaskService(NullLogger<TaskService>.Instance, db, scope),
        };
    }

    private static CreateTaskRequest Create(World w, IEnumerable<string>? owners = null, IEnumerable<string>? assignees = null) =>
        new("Task", ProjectId: w.Project.Id, OwnerEmails: owners, AssigneeEmails: assignees);

    [Fact]
    public async Task Create_assigns_owners_and_assignees_named_by_email_case_insensitively()
    {
        using var w = await BuildAsync();
        var email = new[] { "ravi.kumar@techspear.dev", " MEERA.NAIR@techspear.dev " };

        var created = await w.Tasks.CreateTaskAsync(Create(w, owners: email, assignees: email), w.Admin.Id.ToString());

        Assert.Equal(new HashSet<int> { w.Ravi.Id, w.Meera.Id }, created.Owners.Select(u => u.Id).ToHashSet());
        Assert.Equal(new HashSet<int> { w.Ravi.Id, w.Meera.Id }, created.Assignees.Select(u => u.Id).ToHashSet());
        // the single columns the list views show are filled from the first person named
        Assert.Equal(w.Ravi.Id, created.AssigneeId);
        Assert.Equal(w.Ravi.Id, created.OwnerId);
        Assert.Equal(w.Ravi.Id, created.Assignee!.Id);
    }

    [Fact]
    public async Task An_email_that_is_not_a_pms_user_is_an_error_and_nothing_is_saved()
    {
        using var w = await BuildAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            w.Tasks.CreateTaskAsync(Create(w, owners: ["ravi.kumar@techspear.dev", "ghost@nowhere.com"]), "1"));

        Assert.Contains("ghost@nowhere.com", ex.Message);
        Assert.Empty(w.Db.Tasks);
    }

    [Fact]
    public async Task Update_replaces_the_people_and_moves_the_single_assignee()
    {
        using var w = await BuildAsync();
        var created = await w.Tasks.CreateTaskAsync(Create(w, ["ravi.kumar@techspear.dev"], ["ravi.kumar@techspear.dev"]), "1");

        var updated = (await w.Tasks.UpdateTaskAsync(created.Id,
            new TaskUpdateRequest(OwnerEmails: ["meera.nair@techspear.dev"], AssigneeEmails: ["meera.nair@techspear.dev"]), "1"))!;

        Assert.Equal(new[] { w.Meera.Id }, updated.Assignees.Select(u => u.Id));
        Assert.Equal(new[] { w.Meera.Id }, updated.Owners.Select(u => u.Id));
        Assert.Equal(w.Meera.Id, updated.AssigneeId);
        Assert.Equal(w.Meera.Id, updated.OwnerId);
    }

    [Fact]
    public async Task Update_that_does_not_mention_people_leaves_the_assignment_alone()
    {
        using var w = await BuildAsync();
        var created = await w.Tasks.CreateTaskAsync(Create(w, ["ravi.kumar@techspear.dev"], ["ravi.kumar@techspear.dev"]), "1");

        var updated = (await w.Tasks.UpdateTaskAsync(created.Id, new TaskUpdateRequest(TaskName: "Renamed"), "1"))!;

        Assert.Equal("Renamed", updated.TaskName);
        Assert.Equal(w.Ravi.Id, updated.AssigneeId);
        Assert.Single(updated.Assignees);
    }

    [Fact]
    public async Task Id_based_assignment_still_works_and_is_merged_with_emails_without_duplicates()
    {
        using var w = await BuildAsync();
        var created = await w.Tasks.CreateTaskAsync(
            new("Task", ProjectId: w.Project.Id, AssigneeIds: [w.Ravi.Id], AssigneeEmails: ["ravi.kumar@techspear.dev", "meera.nair@techspear.dev"]), "1");

        Assert.Equal(new HashSet<int> { w.Ravi.Id, w.Meera.Id }, created.Assignees.Select(u => u.Id).ToHashSet());
        Assert.Equal(2, created.Assignees.Count());
    }

    [Fact]
    public async Task A_subtask_can_be_assigned_independently_of_its_parent()
    {
        using var w = await BuildAsync();
        var parent = await w.Tasks.CreateTaskAsync(Create(w, ["ravi.kumar@techspear.dev"], ["ravi.kumar@techspear.dev"]), "1");

        var child = await w.Tasks.CreateTaskAsync(
            new("Child", ParentTaskId: parent.Id, AssigneeEmails: ["meera.nair@techspear.dev"]), "1");

        Assert.Equal(w.Meera.Id, child.AssigneeId);
        Assert.Equal(w.Ravi.Id, (await w.Tasks.GetTaskByIdAsync(parent.Id))!.AssigneeId);
    }
}
