namespace PMS.Application.DTOs.Requests;

// Matches ProjectCreate in PMS-backend/app/schemas/project.py. `PublicId` is server-
// generated (see ProjectService), not accepted from the client — same as Team's "TM-"
// pattern in TeamService.
public record CreateProjectRequest(
    string AccountName,
    string ProjectName,
    string CustomerName,
    string ProjectIdSync,
    string BillingModel,
    string ProjectType,
    DateTime? ExpectedStartDate = null,
    DateTime? ExpectedEndDate = null,
    string? ClientName = null,
    string? Description = null,
    string? ProjectStatusExternal = null,
    string? Tags = null,
    int? OwnerId = null,
    int? ProjectManagerId = null,
    int? DeliveryHeadId = null,
    int? StatusId = null,
    int? PriorityId = null,
    // Nullable, not `decimal = 0m` — pmsfrontend sends an explicit JSON `null` (not a
    // missing field) when the field is left empty (`data.estimated_hours ? ... : null` in
    // ProjectCreateView), and deserializing `null` into a non-nullable `decimal` throws
    // before the request ever reaches this controller, surfacing as a raw 400 with no
    // useful message. Null/missing both mean "treat as zero" — handled in ProjectService.
    decimal? EstimatedHours = null,
    decimal? ActualHours = null,
    DateTime? ActualStartDate = null,
    DateTime? ActualEndDate = null,
    // Named to match pmsfrontend's ProjectCreateView payload field (`user_emails`), not
    // the more descriptive name used elsewhere (e.g. TeamCreateRequest.MemberEmails) —
    // deliberately matching the existing UI's contract rather than the other way round.
    IEnumerable<string>? UserEmails = null
);

// Matches the payload pmsfrontend's ProjectEditView "Save Template" action sends
// (useCloneProjectToTemplate -> projectsService.cloneProjectToTemplate).
public record CloneToTemplateRequest(
    string TemplateName,
    bool IncludeMilestones = false
);
