using PMS.Application.DTOs.Requests;
using PMS.Application.DTOs.Responses;

namespace PMS.Application.Services.Interfaces;

public interface ITaskService
{
    System.Threading.Tasks.Task<TaskResponse> CreateTaskAsync(CreateTaskRequest request, string actorId);
    System.Threading.Tasks.Task<TaskPagedResponseDto> GetTasksAsync(int skip, int limit, int? projectId, int? taskListId, int? statusId, int? assigneeId);
    System.Threading.Tasks.Task<IEnumerable<TaskResponse>> SearchTasksAsync(string query, int limit);
    System.Threading.Tasks.Task<TaskResponse?> GetTaskByIdAsync(int taskId);
    System.Threading.Tasks.Task<TaskResponse?> UpdateTaskAsync(int taskId, TaskUpdateRequest request, string actorId);
    System.Threading.Tasks.Task<bool> DeleteTaskAsync(int taskId, string actorId);
}

public interface ITaskListService
{
    System.Threading.Tasks.Task<TaskListResponse> CreateTaskListAsync(CreateTaskListRequest request, string actorId);
    System.Threading.Tasks.Task<IEnumerable<TaskListResponse>> GetTaskListsAsync(int? projectId);
    System.Threading.Tasks.Task<TaskListResponse?> GetTaskListByIdAsync(int taskListId);
    System.Threading.Tasks.Task<TaskListResponse?> UpdateTaskListAsync(int taskListId, TaskListUpdateRequest request, string actorId);
    System.Threading.Tasks.Task<bool> DeleteTaskListAsync(int taskListId, string actorId);
}
