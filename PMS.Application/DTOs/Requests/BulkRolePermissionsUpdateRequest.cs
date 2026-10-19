using System.Text.Json.Nodes;

namespace PMS.Application.DTOs.Requests;

public record BulkRolePermissionsUpdateRequest(
    Dictionary<int, JsonNode> RolePermissions
);
