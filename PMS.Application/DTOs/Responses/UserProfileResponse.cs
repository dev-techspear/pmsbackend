using System.Text.Json.Nodes;

namespace PMS.Application.DTOs.Responses;

public record UserProfileResponse(
    int Id,
    string PublicId,
    string Email,
    string FirstName,
    string LastName,
    string? DisplayName,
    RoleResponseDto? Role,
    bool IsExternal,
    bool IsSynced
);
