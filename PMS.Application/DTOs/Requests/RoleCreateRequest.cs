using System.Text.Json.Nodes;

namespace PMS.Application.DTOs.Requests;

public record RoleCreateRequest(
    string Name,
    string? Description,
    JsonNode? Permissions
);
