using Microsoft.EntityFrameworkCore;
using PMS.Domain.Constants;
using PMS.Domain.Entities;
using PMS.Infrastructure.Data;

using Task = System.Threading.Tasks.Task;
namespace PMS.Tests;

internal static class TestSupport
{
    public const string FreelancerPermissionsAssociated =
        """{"proj-view":"A","task-view":"A","issue-view":"A","milestone-view":"A","time-view":"A","time-create":true}""";

    public static ApplicationDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    /// <summary>Roles (Admin, Employee, Freelancer) and the Active/Pending statuses.</summary>
    public static async Task<(Role admin, Role employee, Role freelancer, UserStatus active, UserStatus pending)>
        SeedRolesAsync(ApplicationDbContext db, string freelancerPermissions = FreelancerPermissionsAssociated)
    {
        var admin = new Role { Name = AppConstants.RoleAdmin, Permissions = "{}" };
        var employee = new Role { Name = AppConstants.RoleEmployee, Permissions = "{}" };
        var freelancer = new Role { Name = AppConstants.RoleFreelancer, Permissions = freelancerPermissions };
        var active = new UserStatus { Name = AppConstants.UserStatusActive };
        var pending = new UserStatus { Name = AppConstants.UserStatusPending };
        db.Roles.AddRange(admin, employee, freelancer);
        db.UserStatuses.AddRange(active, pending);
        await db.SaveChangesAsync();
        return (admin, employee, freelancer, active, pending);
    }

    public static User NewUser(string email, int? roleId, int? statusId, bool external = false, string? passwordHash = null) => new()
    {
        PublicId = "USR-" + Guid.NewGuid().ToString("N")[..8],
        EmployeeId = "EMP-" + Guid.NewGuid().ToString("N")[..8],
        FirstName = email.Split('@')[0],
        Email = email,
        Username = email.Split('@')[0] + Guid.NewGuid().ToString("N")[..4],
        RoleId = roleId,
        StatusId = statusId,
        IsExternal = external,
        PasswordHash = passwordHash,
    };

    public static Project NewProject(string name, int? ownerId = null, int? managerId = null) => new()
    {
        ProjectIdSync = "SYNC-" + Guid.NewGuid().ToString("N")[..8],
        PublicId = "PRJ-" + Guid.NewGuid().ToString("N")[..8],
        AccountName = "acct",
        ProjectName = name,
        CustomerName = "cust",
        BillingModel = "Fixed",
        ProjectType = "Dev",
        OwnerId = ownerId,
        ProjectManagerId = managerId,
    };

    public static PMS.Domain.Entities.Task NewTask(string name, int? projectId, int? assigneeId = null) => new()
    {
        PublicId = "TSK-" + Guid.NewGuid().ToString("N")[..8],
        TaskName = name,
        ProjectId = projectId,
        AssigneeId = assigneeId,
    };
}
