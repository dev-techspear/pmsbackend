using System;
using System.Collections.Generic;

namespace PMS.Application.DTOs.Responses;

public record UserResponseDto(
    int Id,
    string PublicId,
    string EmployeeId,
    string FirstName,
    string? LastName,
    string Email,
    string Username,
    string? Phone,
    string? JobTitle,
    DateTime? JoinDate,
    int? RoleId,
    int? StatusId,
    string? ManagerEmail,
    string? O365Id,
    string? DisplayName,
    string? Gender,
    string? Country,
    string? State,
    string? Language,
    string? Timezone,
    bool IsExternal,
    bool IsSynced,
    RoleResponseDto? Role,
    MasterResponse? Status,
    UserResponseBaseDto? Manager,
    IEnumerable<SkillResponse> Skills
);
