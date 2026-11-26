using PMS.Application.DTOs.Requests;
using PMS.Application.DTOs.Responses;

namespace PMS.Application.Services.Interfaces;

public interface IProjectLinkService
{
    System.Threading.Tasks.Task<IReadOnlyList<ProjectLinkResponse>> GetLinksAsync(int projectId);
    System.Threading.Tasks.Task<ProjectLinkResponse> CreateLinkAsync(int projectId, CreateProjectLinkRequest request);
    System.Threading.Tasks.Task<ProjectLinkResponse?> UpdateLinkAsync(int projectId, int linkId, UpdateProjectLinkRequest request);
    System.Threading.Tasks.Task<bool> DeleteLinkAsync(int projectId, int linkId);
}

public interface IProjectNoteService
{
    System.Threading.Tasks.Task<IReadOnlyList<ProjectNoteResponse>> GetNotesAsync(int projectId);
    System.Threading.Tasks.Task<ProjectNoteResponse> CreateNoteAsync(int projectId, CreateProjectNoteRequest request);
    System.Threading.Tasks.Task<ProjectNoteResponse?> UpdateNoteAsync(int projectId, int noteId, UpdateProjectNoteRequest request);
    System.Threading.Tasks.Task<bool> DeleteNoteAsync(int projectId, int noteId);
}
