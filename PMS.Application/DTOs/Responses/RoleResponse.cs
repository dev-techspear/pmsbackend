using System.Text.Json.Nodes;

namespace PMS.Application.DTOs.Responses;

public record RoleResponse(
    int Id,
    string Name,
    string? Description,
    JsonNode? Permissions,
    int UsersCount
);
