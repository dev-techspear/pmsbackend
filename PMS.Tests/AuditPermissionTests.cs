using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PMS.Application.Common.Interfaces;
using PMS.Domain.Constants;
using PMS.Domain.Entities;
using PMS.Infrastructure.Data;
using Task = System.Threading.Tasks.Task;

namespace PMS.Tests;

/// <summary>The audit log needs the audit-view permission (Admin always has it), for internal and external users.</summary>
public class AuditPermissionTests
{
    [Fact]
    public async Task Audit_log_is_readable_only_with_audit_view_or_as_admin()
    {
        var dbName = Guid.NewGuid().ToString();
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, cfg) => cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Server=unused;Database=unused;",
            }));
            builder.ConfigureServices(services =>
            {
                services.RemoveAll(typeof(IDbContextOptionsConfiguration<ApplicationDbContext>));
                services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
                services.AddDbContext<ApplicationDbContext>(o => o.UseInMemoryDatabase(dbName));
            });
        });

        var tokens = new Dictionary<string, string>();
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
            var active = await db.UserStatuses.FirstAsync(s => s.Name == "Active");
            var admin = await db.Roles.FirstAsync(r => r.Name == AppConstants.RoleAdmin);
            var without = new Role { Name = "NoAudit", Description = "x", Permissions = "{\"proj-view\":true}" };
            var with = new Role { Name = "WithAudit", Description = "x", Permissions = "{\"audit-view\":true}" };
            db.Roles.AddRange(without, with);
            await db.SaveChangesAsync();

            foreach (var (key, role, external) in new[]
            {
                ("admin", admin, false), ("int-without", without, false), ("int-with", with, false),
                ("ext-without", without, true), ("ext-with", with, true),
            })
            {
                var user = TestSupport.NewUser(key + "@audit.test", role.Id, active.Id, external);
                db.Users.Add(user);
                await db.SaveChangesAsync();
                tokens[key] = tokenService.GenerateAccessToken(user);
            }
        }

        var client = factory.CreateClient();
        async Task<HttpStatusCode> Audit(string who)
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, "/api/v1/audit");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens[who]);
            return (await client.SendAsync(req)).StatusCode;
        }

        Assert.Equal(HttpStatusCode.OK, await Audit("admin"));
        Assert.Equal(HttpStatusCode.OK, await Audit("int-with"));
        Assert.Equal(HttpStatusCode.OK, await Audit("ext-with"));
        Assert.Equal(HttpStatusCode.Forbidden, await Audit("int-without"));
        Assert.Equal(HttpStatusCode.Forbidden, await Audit("ext-without"));
    }

    [Fact]
    public async Task An_existing_admin_role_gets_audit_view_added_without_losing_its_other_permissions()
    {
        using var db = TestSupport.NewContext();
        db.Roles.Add(new Role { Name = AppConstants.RoleAdmin, Description = "x", Permissions = "{\"proj-view\":true}" });
        await db.SaveChangesAsync();

        await DbInitializer.SeedAsync(db);

        var perms = (await db.Roles.SingleAsync(r => r.Name == AppConstants.RoleAdmin)).Permissions;
        Assert.Contains("\"proj-view\":true", perms);
        Assert.Contains("\"audit-view\":true", perms);
        System.Text.Json.JsonDocument.Parse(perms); // still valid JSON
    }
}
