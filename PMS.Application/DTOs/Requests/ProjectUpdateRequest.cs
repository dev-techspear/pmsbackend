namespace PMS.Application.DTOs.Requests;

// All fields optional — matches ProjectUpdate in PMS-backend/app/schemas/project.py.
// `null` means "leave unchanged" (same partial-update convention as UserUpdateRequest /
// TeamUpdateRequest).
public record ProjectUpdateRequest(
    string? AccountName = null,
    string? ProjectName = null,
    string? CustomerName = null,
    string? ClientName = null,
    string? Tags = null,
    string? BillingModel = null,
    string? ProjectType = null,
    string? ProjectStatusExternal = null,
    DateTime? ExpectedStartDate = null,
    DateTime? ExpectedEndDate = null,
    int? OwnerId = null,
    int? ProjectManagerId = null,
    int? DeliveryHeadId = null,
    int? StatusId = null,
    int? PriorityId = null,
    string? Description = null,
    decimal? EstimatedHours = null,
    decimal? ActualHours = null,
    DateTime? ActualStartDate = null,
    DateTime? ActualEndDate = null,
    IEnumerable<string>? UserEmails = null
);
