using PMS.Application.DTOs.Requests;
using PMS.Application.DTOs.Responses;

namespace PMS.Application.Services.Interfaces;

public interface IProjectService
{
    System.Threading.Tasks.Task<ProjectResponse> CreateProjectAsync(CreateProjectRequest request, string actorId);
    System.Threading.Tasks.Task<ProjectListResponseDto> GetProjectsAsync(int skip, int limit, string? search, int? statusId, bool? isArchived);
    System.Threading.Tasks.Task<IEnumerable<ProjectResponse>> SearchProjectsAsync(string query, int limit);
    System.Threading.Tasks.Task<ProjectResponse?> GetProjectByIdAsync(int projectId);
    System.Threading.Tasks.Task<bool> ProjectNameExistsAsync(string projectName, int? excludeProjectId = null);
    System.Threading.Tasks.Task<bool> ProjectIdSyncExistsAsync(string projectIdSync, int? excludeProjectId = null);
    System.Threading.Tasks.Task<ProjectResponse?> UpdateProjectAsync(int projectId, ProjectUpdateRequest request, string actorId);
    System.Threading.Tasks.Task<bool> DeleteProjectAsync(int projectId, string actorId);
    System.Threading.Tasks.Task<ProjectResponse?> SetArchivedAsync(int projectId, bool isArchived, string actorId);

    System.Threading.Tasks.Task<IEnumerable<ProjectMemberResponseDto>> GetProjectMembersAsync(int projectId);
    System.Threading.Tasks.Task<ProjectMemberResponseDto?> AddProjectMemberAsync(int projectId, ProjectMemberRequest request, string actorId);
    System.Threading.Tasks.Task<bool> RemoveProjectMemberAsync(int projectId, int userId, string actorId);

    System.Threading.Tasks.Task<ProjectTemplateResponse?> CloneToTemplateAsync(int projectId, CloneToTemplateRequest request, string actorId);
}
