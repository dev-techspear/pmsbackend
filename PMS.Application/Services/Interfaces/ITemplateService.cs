using PMS.Application.DTOs.Requests;
using PMS.Application.DTOs.Responses;

namespace PMS.Application.Services.Interfaces;

public interface IProjectTemplateService
{
    System.Threading.Tasks.Task<ProjectTemplateResponse> CreateTemplateAsync(CreateProjectTemplateRequest request, string actorId);
    System.Threading.Tasks.Task<ProjectTemplateListResponseDto> GetTemplatesAsync(int skip, int limit);
    System.Threading.Tasks.Task<ProjectTemplateResponse?> GetTemplateByIdAsync(int templateId);
    System.Threading.Tasks.Task<ProjectTemplateResponse?> UpdateTemplateAsync(int templateId, ProjectTemplateUpdateRequest request, string actorId);
    System.Threading.Tasks.Task<bool> DeleteTemplateAsync(int templateId, string actorId);
    System.Threading.Tasks.Task<TemplateTaskResponse?> AddTaskAsync(int templateId, CreateTemplateTaskRequest request);
    System.Threading.Tasks.Task<bool> RemoveTaskAsync(int templateId, int taskId);
}

public interface IAuditService
{
    System.Threading.Tasks.Task<IEnumerable<AuditLogResponse>> GetAuditLogsAsync(int skip, int limit, string? tableName);
}
