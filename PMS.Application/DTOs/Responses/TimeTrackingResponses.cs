namespace PMS.Application.DTOs.Responses;

public record TimeLogResponse(
    int Id,
    string? PublicId,
    string? LogTitle,
    int UserId,
    UserResponseBaseDto? User,
    int? ProjectId,
    int? TaskId,
    int? IssueId,
    int? TimesheetId,
    DateTime Date,
    decimal DailyLogHours,
    string? TimePeriod,
    string? Notes,
    string BillingType,
    int? ApprovalStatusId,
    MasterLookupResponse? ApprovalStatusMaster,
    bool GeneralLog,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record TimeLogPagedResponseDto(
    int Total,
    IEnumerable<TimeLogResponse> Items
);

public record TimesheetResponse(
    int Id,
    string Name,
    DateTime StartDate,
    DateTime EndDate,
    int ProjectId,
    string UserEmail,
    string BillingType,
    decimal TotalHours,
    string ApprovalStatus,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record DocumentResponse(
    int Id,
    string PublicId,
    string Title,
    string? Description,
    string FileUrl,
    string? FileType,
    int? FileSize,
    int ProjectId,
    string? UploadedByEmail,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record ProjectGroupResponse(
    int Id,
    string Name,
    string? Description,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
