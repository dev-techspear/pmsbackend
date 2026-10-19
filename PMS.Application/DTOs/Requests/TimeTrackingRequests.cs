namespace PMS.Application.DTOs.Requests;

public record CreateTimeLogRequest(
    int UserId,
    DateTime Date,
    decimal DailyLogHours,
    int? ProjectId = null,
    int? TaskId = null,
    int? IssueId = null,
    int? TimesheetId = null,
    string? LogTitle = null,
    string? TimePeriod = null,
    string? Notes = null,
    string BillingType = "Billable",
    bool GeneralLog = false
);

public record TimeLogUpdateRequest(
    DateTime? Date = null,
    decimal? DailyLogHours = null,
    string? LogTitle = null,
    string? TimePeriod = null,
    string? Notes = null,
    string? BillingType = null,
    int? ApprovalStatusId = null
);

public record CreateTimesheetRequest(
    string Name,
    DateTime StartDate,
    DateTime EndDate,
    int ProjectId,
    string UserEmail,
    string BillingType = "Billable"
);

public record TimesheetUpdateRequest(
    string? Name = null,
    DateTime? StartDate = null,
    DateTime? EndDate = null,
    string? BillingType = null,
    string? ApprovalStatus = null
);

public record CreateDocumentRequest(
    string Title,
    string FileUrl,
    int ProjectId,
    string? Description = null,
    string? FileType = null,
    int? FileSize = null,
    string? UploadedByEmail = null
);

public record DocumentUpdateRequest(
    string? Title = null,
    string? Description = null
);

public record CreateProjectGroupRequest(
    string Name,
    string? Description = null
);

public record ProjectGroupUpdateRequest(
    string? Name = null,
    string? Description = null
);
