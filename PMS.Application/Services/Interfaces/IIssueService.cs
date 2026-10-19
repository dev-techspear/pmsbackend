using PMS.Application.DTOs.Requests;
using PMS.Application.DTOs.Responses;

namespace PMS.Application.Services.Interfaces;

public interface IIssueService
{
    System.Threading.Tasks.Task<IssueResponse> CreateIssueAsync(CreateIssueRequest request, string actorId);
    System.Threading.Tasks.Task<IssuePagedResponseDto> GetIssuesAsync(int skip, int limit, int? projectId, int? statusId, int? assigneeId, int? severityId);
    System.Threading.Tasks.Task<IEnumerable<IssueResponse>> SearchIssuesAsync(string query, int limit);
    System.Threading.Tasks.Task<IssueResponse?> GetIssueByIdAsync(int issueId);
    System.Threading.Tasks.Task<IssueResponse?> UpdateIssueAsync(int issueId, IssueUpdateRequest request, string actorId);
    System.Threading.Tasks.Task<bool> DeleteIssueAsync(int issueId, string actorId);
}

public interface IMilestoneService
{
    System.Threading.Tasks.Task<MilestoneResponse> CreateMilestoneAsync(CreateMilestoneRequest request, string actorId);
    System.Threading.Tasks.Task<MilestonePagedResponseDto> GetMilestonesAsync(int skip, int limit, int? projectId, int? statusId);
    System.Threading.Tasks.Task<IEnumerable<MilestoneResponse>> SearchMilestonesAsync(string query, int limit);
    System.Threading.Tasks.Task<MilestoneResponse?> GetMilestoneByIdAsync(int milestoneId);
    System.Threading.Tasks.Task<MilestoneResponse?> UpdateMilestoneAsync(int milestoneId, MilestoneUpdateRequest request, string actorId);
    System.Threading.Tasks.Task<bool> DeleteMilestoneAsync(int milestoneId, string actorId);
}
