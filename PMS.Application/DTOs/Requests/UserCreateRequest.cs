using System;
using System.Collections.Generic;

namespace PMS.Application.DTOs.Requests;

public record UserCreateRequest(
    string FirstName,
    string? LastName,
    string Email,
    string? Username,
    bool IsExternal,
    bool IsSynced,
    string? DisplayName,
    string? Gender,
    string? Country,
    string? State,
    string? Language,
    string? Timezone,
    string? EmployeeId,
    string? Phone,
    string? JobTitle,
    DateTime? JoinDate,
    int? RoleId,
    int? StatusId,
    IEnumerable<int>? SkillIds,
    string? O365Id,
    string? ManagerEmail
);
