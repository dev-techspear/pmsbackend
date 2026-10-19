using PMS.Application.DTOs.Requests;
using PMS.Application.DTOs.Responses;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PMS.Application.Services.Interfaces;

public interface ITeamService
{
    Task<TeamResponseDto> CreateTeamAsync(TeamCreateRequest request, string actorId);
    Task<TeamListResponseDto> GetTeamsAsync(int skip, int limit);
    Task<IEnumerable<TeamResponseDto>> SearchTeamsAsync(string query, int limit);
    Task<TeamWithMembersResponseDto?> GetTeamByIdAsync(int teamId);
    Task<TeamResponseDto?> UpdateTeamAsync(int teamId, TeamUpdateRequest request, string actorId);
    Task<bool> DeleteTeamAsync(int teamId, string actorId);
    Task<bool> AddTeamMemberAsync(int teamId, string userEmail, string actorId);
    Task<bool> RemoveTeamMemberAsync(int teamId, string userEmail, string actorId);
}
