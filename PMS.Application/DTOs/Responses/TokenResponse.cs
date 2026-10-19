using System.Text.Json.Nodes;

namespace PMS.Application.DTOs.Responses;

public record TokenResponse(
    string AccessToken,
    string RefreshToken,
    string TokenType,
    int UserId,
    string PublicId,
    string Email,
    string FirstName,
    string LastName,
    string? DisplayName,
    RoleResponseDto? Role,
    bool IsExternal,
    bool IsSynced
);

public record RoleResponseDto(
    int Id,
    string Name,
    JsonNode? Permissions
);
