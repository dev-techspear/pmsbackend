namespace PMS.Application.DTOs.Responses;

public record IssueResponse(
    int Id,
    string PublicId,
    string BugName,
    string? Description,
    int? ProjectId,
    int? MilestoneId,
    int? AssociatedTeamId,
    int? ReporterId,
    UserResponseBaseDto? Reporter,
    int? AssigneeId,
    UserResponseBaseDto? Assignee,
    int? StatusId,
    int? PriorityId,
    int? SeverityId,
    int? ClassificationId,
    MasterLookupResponse? StatusMaster,
    MasterLookupResponse? PriorityMaster,
    MasterLookupResponse? SeverityMaster,
    MasterLookupResponse? ClassificationMaster,
    string? Module,
    string? Tags,
    string? Flag,
    bool ReproducibleFlag,
    DateTime? StartDate,
    DateTime? DueDate,
    decimal? EstimatedHours,
    IEnumerable<UserResponseBaseDto> Followers,
    IEnumerable<UserResponseBaseDto> Assignees,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record IssuePagedResponseDto(
    int Total,
    IEnumerable<IssueResponse> Items
);

public record MilestoneResponse(
    int Id,
    string PublicId,
    string MilestoneName,
    string? Description,
    int? ProjectId,
    int? OwnerId,
    UserResponseBaseDto? Owner,
    int? StatusId,
    int? PriorityId,
    MasterLookupResponse? StatusMaster,
    MasterLookupResponse? PriorityMaster,
    string? Flags,
    string? Tags,
    DateTime? StartDate,
    DateTime? EndDate,
    int CompletionPercentage,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record MilestonePagedResponseDto(
    int Total,
    IEnumerable<MilestoneResponse> Items
);
