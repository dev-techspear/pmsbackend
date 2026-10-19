using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PMS.Application.Common.Interfaces;
using PMS.Domain.Constants;

namespace PMS.Application.Common;

public sealed class AccessScope : IAccessScope
{
    private readonly IApplicationDbContext _context;
    private readonly Dictionary<string, string> _levels = new();

    public AccessScope(IApplicationDbContext context)
    {
        _context = context;
    }

    public bool IsLoaded { get; private set; }
    public int? UserId { get; private set; }
    public string? Email { get; private set; }
    public bool IsAdmin { get; private set; }
    public bool IsExternal { get; private set; }

    public async Task LoadAsync(int userId)
    {
        var user = await _context.Users
            .AsNoTracking()
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == userId);

        _levels.Clear();
        UserId = userId;
        IsLoaded = true;
        if (user == null) return;

        Email = user.Email;
        IsExternal = user.IsExternal;
        IsAdmin = string.Equals(user.Role?.Name, AppConstants.RoleAdmin, StringComparison.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(user.Role?.Permissions)) return;
        try
        {
            using var doc = JsonDocument.Parse(user.Role.Permissions);
            if (doc.RootElement.ValueKind != JsonValueKind.Object) return;
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                // Same value shapes the frontend accepts: true, "All", "O", "A"; anything else = no access.
                var level = prop.Value.ValueKind switch
                {
                    JsonValueKind.True => AppConstants.ViewLevelAll,
                    JsonValueKind.String => prop.Value.GetString(),
                    _ => null,
                };
                if (level is AppConstants.ViewLevelAll or AppConstants.ViewLevelOwned or AppConstants.ViewLevelAssociated)
                {
                    _levels[prop.Name] = level;
                }
            }
        }
        catch (JsonException)
        {
            // Malformed permission JSON grants nothing.
        }
    }

    public string? Level(string permissionId)
    {
        if (IsAdmin) return AppConstants.ViewLevelAll;
        return _levels.TryGetValue(permissionId, out var level) ? level : null;
    }

    public bool IsRestricted(string permissionId)
    {
        if (!IsLoaded || UserId == null || IsAdmin) return false;
        var level = Level(permissionId);
        if (level == AppConstants.ViewLevelAll) return false;
        return level != null || IsExternal;
    }
}
