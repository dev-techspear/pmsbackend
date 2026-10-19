using System.Text.Json.Nodes;

namespace PMS.Application.DTOs.Requests;

public record RoleUpdateRequest(
    string? Name,
    string? Description,
    JsonNode? Permissions
);
