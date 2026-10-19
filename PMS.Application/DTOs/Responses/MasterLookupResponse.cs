namespace PMS.Application.DTOs.Responses;

public record MasterLookupResponse(
    int Id,
    string Category,
    string Value,
    string Label,
    string? Color,
    string? Icon,
    int OrderIndex,
    bool IsActive
);
