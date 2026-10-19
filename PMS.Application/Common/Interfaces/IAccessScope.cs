namespace PMS.Application.Common.Interfaces;

/// <summary>
/// What the current request's user is allowed to see, resolved from their role's permission JSON.
/// Loaded once per request by the API's access filter; services read it synchronously.
/// </summary>
public interface IAccessScope
{
    /// <summary>False until <see cref="LoadAsync"/> ran (e.g. anonymous requests, background work).</summary>
    bool IsLoaded { get; }
    int? UserId { get; }
    string? Email { get; }
    bool IsAdmin { get; }
    bool IsExternal { get; }

    Task LoadAsync(int userId);

    /// <summary>"All", "O" (owned), "A" (associated) or null when the role has no such permission.</summary>
    string? Level(string permissionId);

    /// <summary>
    /// True when results for this permission must be filtered to the user's own/associated records.
    /// Internal users with no value for a permission stay unrestricted (legacy behaviour);
    /// external users are restricted by default.
    /// </summary>
    bool IsRestricted(string permissionId);
}
