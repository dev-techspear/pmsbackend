namespace PMS.Application.DTOs.Requests;

public record CreateProjectLinkRequest(
    string Title,
    string Url,
    string LinkType,
    string? Description = null,
    bool RestrictAccess = false,
    IReadOnlyList<int>? RoleIds = null,
    IReadOnlyList<int>? UserIds = null
);

/// <summary>Null fields stay unchanged. RoleIds/UserIds null = keep the current shares.</summary>
public record UpdateProjectLinkRequest(
    string? Title = null,
    string? Url = null,
    string? LinkType = null,
    string? Description = null,
    bool? RestrictAccess = null,
    IReadOnlyList<int>? RoleIds = null,
    IReadOnlyList<int>? UserIds = null
);

public record CreateProjectNoteRequest(
    string Body,
    string? Title = null,
    bool IsPinned = false,
    bool RestrictAccess = false,
    IReadOnlyList<int>? RoleIds = null,
    IReadOnlyList<int>? UserIds = null
);

public record UpdateProjectNoteRequest(
    string? Body = null,
    string? Title = null,
    bool? IsPinned = null,
    bool? RestrictAccess = null,
    IReadOnlyList<int>? RoleIds = null,
    IReadOnlyList<int>? UserIds = null
);
