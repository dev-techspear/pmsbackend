namespace PMS.Application.DTOs.Responses;

public record ProjectLinkResponse(
    int Id,
    int ProjectId,
    string Title,
    string Url,
    string LinkType,
    string? Description,
    bool RestrictAccess,
    IReadOnlyList<int> SharedRoleIds,
    IReadOnlyList<int> SharedUserIds,
    string? CreatedByName,
    DateTime CreatedAt,
    bool CanManage
);

public record ProjectNoteResponse(
    int Id,
    int ProjectId,
    string? Title,
    string Body,
    bool IsPinned,
    bool RestrictAccess,
    IReadOnlyList<int> SharedRoleIds,
    IReadOnlyList<int> SharedUserIds,
    string? AuthorName,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    bool CanManage
);
