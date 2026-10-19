namespace PMS.Application.DTOs.Responses;

// Matches ProjectResponse in PMS-backend/app/schemas/project.py — field for field, so the
// frontend's existing shape expectations hold once pmsfrontend points here instead of
// PMS-backend.
public record ProjectResponse(
    int Id,
    string PublicId,
    string ProjectIdSync,
    string AccountName,
    string ProjectName,
    string CustomerName,
    string? ClientName,
    string? Tags,
    string BillingModel,
    string ProjectType,
    string? ProjectStatusExternal,
    DateTime? ExpectedStartDate,
    DateTime? ExpectedEndDate,
    string? Description,
    int? StatusId,
    int? PriorityId,
    MasterLookupResponse? StatusMaster,
    MasterLookupResponse? PriorityMaster,
    int? OwnerId,
    UserResponseBaseDto? Owner,
    int? ProjectManagerId,
    UserResponseBaseDto? ProjectManager,
    int? DeliveryHeadId,
    UserResponseBaseDto? DeliveryHead,
    decimal EstimatedHours,
    decimal ActualHours,
    DateTime? ActualStartDate,
    DateTime? ActualEndDate,
    bool IsArchived,
    bool IsTemplate,
    bool IsGroup,
    int MemberCount,
    int TaskCount,
    int IssueCount,
    int MilestoneCount,
    IEnumerable<ProjectMemberResponseDto> TeamMembers,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record ProjectListResponseDto(
    int Total,
    IEnumerable<ProjectResponse> Items
);

// Matches ProjectMemberResponse in PMS-backend/app/schemas/project.py.
public record ProjectMemberResponseDto(
    int ProjectId,
    int UserId,
    string? ProjectProfile,
    string? PortalProfile,
    string? RoleInProject,
    int? InvitationStatusId,
    bool IsOwner,
    DateTime CreatedAt,
    UserResponseBaseDto? User
);
