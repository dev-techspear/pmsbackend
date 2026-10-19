using PMS.Application.DTOs.Requests;
using PMS.Application.DTOs.Responses;

namespace PMS.Application.Services.Interfaces;

public interface IUserService
{
    Task<UserListResponseDto> GetUsersAsync(int skip, int limit, string? search, IEnumerable<int>? roleIds, bool? isActive);
    Task<UserResponseDto?> GetUserByIdAsync(int id);
    Task<UserResponseDto?> GetUserByEmailAsync(string email);
    Task<UserResponseDto?> GetUserByUsernameAsync(string username);
    Task<UserResponseDto> CreateUserAsync(UserCreateRequest request, string actorId);
    Task<UserResponseDto?> UpdateUserAsync(int id, UserUpdateRequest request, string actorId);
    Task<bool> DeleteUserAsync(int id, string actorId);
    Task<IEnumerable<UserResponseDto>> SearchUsersAsync(string? query, int limit, bool? isActive);
    Task<SyncUsersResponse> SyncUsersFromAzureAsync();
    Task<IEnumerable<AzureUserSearchResult>> SearchAzureUsersAsync(string query, int limit);
}
