namespace PMS.Application.DTOs.Requests;

public record CreateProjectTemplateRequest(
    string Name,
    string? Description = null,
    string? BillingType = null,
    bool IsPublic = true,
    int? SourceProjectId = null
);

public record ProjectTemplateUpdateRequest(
    string? Name = null,
    string? Description = null,
    string? BillingType = null,
    bool? IsPublic = null
);

public record CreateTemplateTaskRequest(
    string Title,
    string? Description = null,
    decimal? EstimatedHours = null,
    int? Duration = null,
    string? BillingType = null,
    string? Tags = null,
    int OrderIndex = 0
);
