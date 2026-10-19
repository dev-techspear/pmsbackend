namespace PMS.Application.DTOs.Requests;

public record CreateTaskRequest(
    string TaskName,
    int? ProjectId = null,
    int? TaskListId = null,
    int? MilestoneId = null,
    int? AssociatedTeamId = null,
    int? AssigneeId = null,
    int? OwnerId = null,
    int? StatusId = null,
    int? PriorityId = null,
    string? Description = null,
    string? Tags = null,
    DateTime? StartDate = null,
    DateTime? DueDate = null,
    int? Duration = null,
    decimal? EstimatedHours = null,
    string BillingType = "Billable",
    IEnumerable<int>? OwnerIds = null,
    IEnumerable<int>? AssigneeIds = null
);

public record TaskUpdateRequest(
    string? TaskName = null,
    int? TaskListId = null,
    int? MilestoneId = null,
    int? AssociatedTeamId = null,
    int? AssigneeId = null,
    int? OwnerId = null,
    int? StatusId = null,
    int? PriorityId = null,
    string? Description = null,
    string? Tags = null,
    DateTime? StartDate = null,
    DateTime? DueDate = null,
    DateTime? CompletionDate = null,
    int? Duration = null,
    int? CompletionPercentage = null,
    decimal? EstimatedHours = null,
    decimal? WorkHours = null,
    string? BillingType = null,
    IEnumerable<int>? OwnerIds = null,
    IEnumerable<int>? AssigneeIds = null
);

public record CreateTaskListRequest(
    int ProjectId,
    string Name,
    int? MilestoneId = null,
    string? Description = null
);

public record TaskListUpdateRequest(
    string? Name = null,
    int? MilestoneId = null,
    string? Description = null
);
