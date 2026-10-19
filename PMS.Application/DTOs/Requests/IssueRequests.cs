namespace PMS.Application.DTOs.Requests;

public record CreateIssueRequest(
    string BugName,
    int? ProjectId = null,
    int? MilestoneId = null,
    int? AssociatedTeamId = null,
    int? ReporterId = null,
    int? AssigneeId = null,
    int? StatusId = null,
    int? PriorityId = null,
    int? SeverityId = null,
    int? ClassificationId = null,
    string? Description = null,
    string? Module = null,
    string? Tags = null,
    string? Flag = null,
    bool ReproducibleFlag = true,
    DateTime? StartDate = null,
    DateTime? DueDate = null,
    decimal? EstimatedHours = null,
    IEnumerable<int>? FollowerIds = null,
    IEnumerable<int>? AssigneeIds = null
);

public record IssueUpdateRequest(
    string? BugName = null,
    int? MilestoneId = null,
    int? AssociatedTeamId = null,
    int? ReporterId = null,
    int? AssigneeId = null,
    int? StatusId = null,
    int? PriorityId = null,
    int? SeverityId = null,
    int? ClassificationId = null,
    string? Description = null,
    string? Module = null,
    string? Tags = null,
    string? Flag = null,
    bool? ReproducibleFlag = null,
    DateTime? StartDate = null,
    DateTime? DueDate = null,
    decimal? EstimatedHours = null,
    IEnumerable<int>? FollowerIds = null,
    IEnumerable<int>? AssigneeIds = null
);

public record CreateMilestoneRequest(
    string MilestoneName,
    int? ProjectId = null,
    int? OwnerId = null,
    int? StatusId = null,
    int? PriorityId = null,
    string? Description = null,
    string? Flags = null,
    string? Tags = null,
    DateTime? StartDate = null,
    DateTime? EndDate = null
);

public record MilestoneUpdateRequest(
    string? MilestoneName = null,
    int? OwnerId = null,
    int? StatusId = null,
    int? PriorityId = null,
    string? Description = null,
    string? Flags = null,
    string? Tags = null,
    DateTime? StartDate = null,
    DateTime? EndDate = null,
    int? CompletionPercentage = null
);
