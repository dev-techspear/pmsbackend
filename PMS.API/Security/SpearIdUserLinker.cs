using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using PMS.Domain.Constants;
using PMS.Domain.Entities;
using PMS.Infrastructure.Data;
using Techspear.Identity.Adapter;

namespace PMS.API.Security;

/// <summary>
/// Turns a Spear ID subject into the local integer user id PMS already stores on every row.
/// </summary>
public sealed class SpearIdUserLinker
{
    private readonly ApplicationDbContext _db;

    public SpearIdUserLinker(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<ClaimsPrincipal> LinkAsync(SpearIdentity identity, CancellationToken ct)
    {
        var email = identity.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.IdentityId == identity.Subject, ct);
        if (user == null && email.Length > 0)
        {
            user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
        }

        if (user == null)
        {
            var role = await _db.Roles.FirstOrDefaultAsync(r => r.Name == identity.Role, ct)
                ?? await _db.Roles.FirstOrDefaultAsync(r => r.Name == AppConstants.RoleEmployee, ct);
            var status = await _db.UserStatuses.FirstOrDefaultAsync(s => s.Name == AppConstants.UserStatusActive, ct);
            var local = email.Length > 0 ? email.Split('@')[0] : identity.Subject.ToString("N")[..8];
            var username = local.Replace('.', '_');
            if (await _db.Users.AnyAsync(u => u.Username == username, ct))
            {
                username = username + "_" + Guid.NewGuid().ToString("N")[..8];
            }

            user = new User
            {
                PublicId = "USR-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant(),
                EmployeeId = "EMP-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant(),
                FirstName = FirstName(identity.Name, local),
                Email = email.Length > 0 ? email : identity.Subject.ToString("N") + "@identity.local",
                Username = username,
                DisplayName = string.IsNullOrWhiteSpace(identity.Name) ? local : identity.Name.Trim(),
                IdentityId = identity.Subject,
                IsExternal = string.Equals(identity.UserType, "EXTERNAL", StringComparison.OrdinalIgnoreCase),
                RoleId = role?.Id,
                StatusId = status?.Id,
            };
            _db.Users.Add(user);
        }
        else
        {
            user.IdentityId = identity.Subject;
        }

        await _db.SaveChangesAsync(ct);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Role, identity.Role),
            new("utype", identity.UserType),
        };
        claims.AddRange(identity.Permissions.Select(p => new Claim("perm", p)));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "SpearId"));
    }

    private static string FirstName(string name, string fallback)
    {
        var trimmed = name.Trim();
        if (trimmed.Length == 0) return fallback;
        var space = trimmed.IndexOf(' ');
        return space > 0 ? trimmed[..space] : trimmed;
    }
}
