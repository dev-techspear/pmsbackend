namespace PMS.Application.DTOs.Responses;

public record DocumentDownload(string FileName, string Url);

public record CredentialResponse(
    int Id,
    int ProjectId,
    string Label,
    string CredentialType,
    string? Host,
    string? Username,
    string? Notes,
    bool RestrictAccess,
    IReadOnlyList<int> SharedRoleIds,
    IReadOnlyList<int> SharedUserIds,
    DateTime? LastRotatedAt,
    DateTime CreatedAt,
    bool CanManage
);

public record CredentialSecretResponse(string Secret);
