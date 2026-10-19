using Microsoft.EntityFrameworkCore;
using PMS.Domain.Entities;

namespace PMS.Infrastructure.Data;

/// <summary>
/// Seeds reference/lookup data — roles, user statuses, skills, and the master_lookups
/// categories everything else (projects, tasks, issues, milestones) hangs its status/
/// priority/severity off. Ported 1:1 from PMS-backend/app/core/seeding.py's seed_all()
/// so the two apps agree on the same canonical values. Idempotent: every insert is guarded
/// by an existence check, same as the source, so this is safe to call on every startup.
/// </summary>
public static class DbInitializer
{
    public static async System.Threading.Tasks.Task SeedAsync(ApplicationDbContext context)
    {
        await SeedUserStatusesAsync(context);
        await SeedSkillsAsync(context);
        await SeedRolesAsync(context);
        await SeedMasterLookupsAsync(context);

        await context.SaveChangesAsync();
    }

    private static async System.Threading.Tasks.Task SeedUserStatusesAsync(ApplicationDbContext context)
    {
        string[] names = ["Active", "Inactive", "Pending", "Onboarding", "On Leave"];
        foreach (var name in names)
        {
            if (!await context.UserStatuses.AnyAsync(u => u.Name == name))
            {
                context.UserStatuses.Add(new UserStatus { Name = name });
            }
        }
    }

    private static async System.Threading.Tasks.Task SeedSkillsAsync(ApplicationDbContext context)
    {
        string[] names =
        [
            "React", "Python", "FastAPI", "UI/UX Design", "Node.js", ".NET", "DevOps",
            "Project Management", "Data Analytics"
        ];
        foreach (var name in names)
        {
            if (!await context.Skills.AnyAsync(s => s.Name == name))
            {
                context.Skills.Add(new Skill { Name = name });
            }
        }
    }

    private static async System.Threading.Tasks.Task SeedRolesAsync(ApplicationDbContext context)
    {
        // Matches seed_roles() in the source exactly: four canonical roles, full permissions
        // only on Admin, everyone else starts with an empty permission set.
        const string adminPermissions = """
        {"proj-view":true,"proj-create":true,"proj-edit":true,"proj-delete":true,
         "task-view":true,"task-create":true,"task-edit":true,"task-delete":true,"task-assign":true,
         "issue-view":true,"issue-create":true,"issue-edit":true,"issue-delete":true,
         "milestone-view":true,"milestone-create":true,"milestone-edit":true,"milestone-delete":true,
         "time-view":true,"time-create":true,"time-edit":true,"time-delete":true,"timesheet-approve":true,
         "user-view":true,"user-create":true,"user-edit":true,"user-delete":true,
         "team-view":true,"team-create":true,"team-edit":true,"team-delete":true,
         "report-view":true,"report-export":true,
         "settings-view":true,"settings-edit":true,"role-manage":true,"automation-manage":true}
        """;

        // "Freelancer" is the restricted role for external (email + password) users: empty permission set.
        string[] canonicalRoles = ["Admin", "Team Lead", "Project Manager", "Employee", "Freelancer"];

        foreach (var roleName in canonicalRoles)
        {
            var role = await context.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
            if (role == null)
            {
                context.Roles.Add(new Role
                {
                    Name = roleName,
                    Description = $"{roleName} role",
                    Permissions = roleName == "Admin" ? adminPermissions : "{}"
                });
            }
            else if (roleName == "Admin" && (role.Permissions == "{}" || string.IsNullOrWhiteSpace(role.Permissions)))
            {
                role.Permissions = adminPermissions;
            }
        }
    }

    private static async System.Threading.Tasks.Task SeedMasterLookupsAsync(ApplicationDbContext context)
    {
        // Category -> ordered values, verbatim from seed_master_lookups() in the source.
        var lookups = new Dictionary<string, string[]>
        {
            ["ProjectStatus"] = ["Planning", "Active", "In Progress", "On Hold", "Completed", "Closed", "Cancelled"],
            ["TaskStatus"] = ["Open", "In Progress", "In Review", "Completed", "Cancelled", "On Hold"],
            ["TaskPriority"] = ["Low", "Medium", "High", "Critical"],
            ["IssueStatus"] = ["Open", "In Progress", "Hold", "Cancelled", "Resolved", "Re-Open", "Not a defect", "Closed"],
            ["IssueSeverity"] = ["Low", "Medium", "High", "Critical", "Blocker", "Show Stopper"],
            ["IssueClassification"] = ["None", "Security", "Crash/Hang", "Data Loss", "Performance", "UI/UX Usability", "Other Bugs", "Feature (New)", "Enhancement"],
            ["ProjectType"] = ["Internal", "External"],
            ["ProjectBillingModel"] = ["T&M", "Fixed Price", "Retainer", "Non-Billable"],
            ["TaskBillingType"] = ["Billable", "Non-Billable", "Internal"],
            ["MilestoneStatus"] = ["Planning", "Active", "In Progress", "On Hold", "Completed", "Cancelled"],
        };

        foreach (var (category, values) in lookups)
        {
            for (var idx = 0; idx < values.Length; idx++)
            {
                var value = values[idx];
                var exists = await context.MasterLookups.AnyAsync(
                    m => m.Category == category && m.Value == value);
                if (!exists)
                {
                    context.MasterLookups.Add(new MasterLookup
                    {
                        Category = category,
                        Value = value,
                        Label = value,
                        OrderIndex = idx
                    });
                }
            }
        }
    }
}
