using PMS.Application.DTOs.Responses;
using PMS.Application.DTOs.Requests;

namespace PMS.Application.Services.Interfaces;

public interface IMasterService
{
    Task<IEnumerable<MasterLookupResponse>> GetMasterLookupsAsync(string category);
    Task<IEnumerable<MasterLookupResponse>> SearchMasterLookupsAsync(string category, string query, int limit);

    Task<IEnumerable<MasterResponse>> GetUserStatusesAsync();
    Task<IEnumerable<MasterResponse>> SearchUserStatusesAsync(string query, int limit);

    Task<IEnumerable<MasterResponse>> GetStatusesAsync();
    Task<IEnumerable<MasterResponse>> SearchStatusesAsync(string query, int limit);

    Task<IEnumerable<MasterResponse>> GetPrioritiesAsync();
    Task<IEnumerable<MasterResponse>> SearchPrioritiesAsync(string query, int limit);

    Task<IEnumerable<RoleResponse>> GetRolesAsync();
    Task<IEnumerable<RoleResponse>> SearchRolesAsync(string query, int limit);
    Task<RoleResponse?> GetRoleByIdAsync(int roleId);
    Task<RoleResponse> CreateRoleAsync(RoleCreateRequest request);
    Task<RoleResponse?> UpdateRoleAsync(int roleId, RoleUpdateRequest request);
    Task<bool> DeleteRoleAsync(int roleId);
    Task UpdateBulkRolePermissionsAsync(BulkRolePermissionsUpdateRequest request);

    Task<IEnumerable<SkillResponse>> GetSkillsAsync();
    Task<IEnumerable<SkillResponse>> SearchSkillsAsync(string query, int limit);
}
