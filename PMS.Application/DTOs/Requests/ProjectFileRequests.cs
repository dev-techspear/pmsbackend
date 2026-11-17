namespace PMS.Application.DTOs.Requests;

/// <summary>Metadata sent with a multipart document upload.</summary>
public record UploadDocumentRequest(
    int ProjectId,
    string? Title,
    string? Description,
    bool RestrictAccess,
    IReadOnlyList<int> RoleIds,
    IReadOnlyList<int> UserIds
);

public record DocumentUploadFile(string FileName, string ContentType, byte[] Content);

public record DocumentSharingRequest(
    bool RestrictAccess,
    IReadOnlyList<int>? RoleIds = null,
    IReadOnlyList<int>? UserIds = null
);

public record CreateCredentialRequest(
    string Label,
    string CredentialType,
    string Secret,
    string? Host = null,
    string? Username = null,
    string? Notes = null,
    bool RestrictAccess = true,
    IReadOnlyList<int>? RoleIds = null,
    IReadOnlyList<int>? UserIds = null
);

/// <summary>Null fields are left unchanged. A non-null Secret rotates the stored secret.</summary>
public record UpdateCredentialRequest(
    string? Label = null,
    string? CredentialType = null,
    string? Secret = null,
    string? Host = null,
    string? Username = null,
    string? Notes = null,
    bool? RestrictAccess = null,
    IReadOnlyList<int>? RoleIds = null,
    IReadOnlyList<int>? UserIds = null
);
