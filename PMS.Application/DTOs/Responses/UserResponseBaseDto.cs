namespace PMS.Application.DTOs.Responses;

public record UserResponseBaseDto(
    int Id,
    string PublicId,
    string Email,
    string Username,
    string FirstName,
    string? LastName,
    string? DisplayName,
    bool IsExternal,
    bool IsSynced
);
