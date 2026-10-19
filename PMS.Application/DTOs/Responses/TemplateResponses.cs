namespace PMS.Application.DTOs.Responses;

public record TemplateTaskResponse(
    int Id,
    int TemplateId,
    string Title,
    string? Description,
    decimal? EstimatedHours,
    int? Duration,
    string? BillingType,
    string? Tags,
    int OrderIndex
);

public record ProjectTemplateResponse(
    int Id,
    string? PublicId,
    string Name,
    string? Description,
    string? BillingType,
    bool IsPublic,
    int? SourceProjectId,
    int? CreatedById,
    IEnumerable<TemplateTaskResponse> Tasks,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record ProjectTemplateListResponseDto(
    int Total,
    IEnumerable<ProjectTemplateResponse> Items
);

public record AuditLogResponse(
    long Id,
    string TableName,
    int Action,
    Guid PerformedBy,
    DateTime PerformedOn,
    string? RecordName,
    Guid TransactionId,
    string? Comments,
    string? ModuleName
);
