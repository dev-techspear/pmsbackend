using PMS.Application.DTOs.Requests;
using PMS.Application.DTOs.Responses;

namespace PMS.Application.Services.Interfaces;

public interface ITimeLogService
{
    System.Threading.Tasks.Task<TimeLogResponse> CreateTimeLogAsync(CreateTimeLogRequest request, string actorId);
    System.Threading.Tasks.Task<TimeLogPagedResponseDto> GetTimeLogsAsync(int skip, int limit, int? projectId, int? taskId, int? userId, int? issueId);
    System.Threading.Tasks.Task<TimeLogResponse?> GetTimeLogByIdAsync(int timeLogId);
    System.Threading.Tasks.Task<TimeLogResponse?> UpdateTimeLogAsync(int timeLogId, TimeLogUpdateRequest request, string actorId);
    System.Threading.Tasks.Task<bool> DeleteTimeLogAsync(int timeLogId, string actorId);
}

public interface ITimesheetService
{
    System.Threading.Tasks.Task<TimesheetResponse> CreateTimesheetAsync(CreateTimesheetRequest request, string actorId);
    System.Threading.Tasks.Task<IEnumerable<TimesheetResponse>> GetTimesheetsAsync(int? projectId, string? userEmail);
    System.Threading.Tasks.Task<TimesheetResponse?> GetTimesheetByIdAsync(int timesheetId);
    System.Threading.Tasks.Task<TimesheetResponse?> UpdateTimesheetAsync(int timesheetId, TimesheetUpdateRequest request, string actorId);
    System.Threading.Tasks.Task<bool> DeleteTimesheetAsync(int timesheetId, string actorId);
}

public interface IDocumentService
{
    System.Threading.Tasks.Task<DocumentResponse> CreateDocumentAsync(CreateDocumentRequest request, string actorId);
    System.Threading.Tasks.Task<IEnumerable<DocumentResponse>> GetDocumentsAsync(int? projectId);
    System.Threading.Tasks.Task<DocumentResponse?> GetDocumentByIdAsync(int documentId);
    System.Threading.Tasks.Task<DocumentResponse?> UpdateDocumentAsync(int documentId, DocumentUpdateRequest request, string actorId);
    System.Threading.Tasks.Task<bool> DeleteDocumentAsync(int documentId, string actorId);
}

public interface IProjectGroupService
{
    System.Threading.Tasks.Task<ProjectGroupResponse> CreateProjectGroupAsync(CreateProjectGroupRequest request, string actorId);
    System.Threading.Tasks.Task<IEnumerable<ProjectGroupResponse>> GetProjectGroupsAsync();
    System.Threading.Tasks.Task<ProjectGroupResponse?> GetProjectGroupByIdAsync(int projectGroupId);
    System.Threading.Tasks.Task<ProjectGroupResponse?> UpdateProjectGroupAsync(int projectGroupId, ProjectGroupUpdateRequest request, string actorId);
    System.Threading.Tasks.Task<bool> DeleteProjectGroupAsync(int projectGroupId, string actorId);
}
