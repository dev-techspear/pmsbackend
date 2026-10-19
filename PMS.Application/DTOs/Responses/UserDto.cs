namespace PMS.Application.DTOs.Responses;

public record UserDto(
    int Id,
    string PublicId,
    string EmployeeId,
    string FirstName,
    string? LastName,
    string Email,
    string Username,
    string? Phone,
    string? JobTitle,
    int? RoleId,
    string? RoleName,
    int? StatusId,
    string? StatusName,
    string? ManagerEmail
);
