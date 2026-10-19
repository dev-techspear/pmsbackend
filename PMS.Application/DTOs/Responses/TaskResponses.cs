namespace PMS.Application.DTOs.Responses;

// Note: deliberately NOT named `TaskListResponse` for a paged collection of Tasks — the
// source Python schemas reuse that name for two different things (a `Task` page AND the
// `TaskList` bucket entity) via module-scoped imports, which C# can't shadow the same way.
// `TaskResponse` = one task. `TaskPagedResponseDto` = a page of tasks.
// `TaskListResponse` = the `TaskList` bucket entity (see below).
public record TaskResponse(
    int Id,
    string PublicId,
    string TaskName,
    string? Description,
    int? ProjectId,
    int? TaskListId,
    int? MilestoneId,
    int? AssociatedTeamId,
    int? AssigneeId,
    UserResponseBaseDto? Assignee,
    int? CreatedById,
    int? OwnerId,
    int? StatusId,
    int? PriorityId,
    MasterLookupResponse? StatusMaster,
    MasterLookupResponse? PriorityMaster,
    string? Tags,
    DateTime? StartDate,
    DateTime? DueDate,
    DateTime? CompletionDate,
    int? Duration,
    int CompletionPercentage,
    decimal? EstimatedHours,
    decimal? WorkHours,
    decimal CachedTimelogTotal,
    string BillingType,
    IEnumerable<UserResponseBaseDto> Owners,
    IEnumerable<UserResponseBaseDto> Assignees,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record TaskPagedResponseDto(
    int Total,
    IEnumerable<TaskResponse> Items
);

public record TaskListResponse(
    int Id,
    int ProjectId,
    int? MilestoneId,
    string Name,
    string? Description,
    int TaskCount,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
