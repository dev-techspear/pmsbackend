using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PMS.Application.Common;
using PMS.Application.Common.Interfaces;
using PMS.Application.DTOs.Requests;
using PMS.Application.DTOs.Responses;
using PMS.Application.Services.Interfaces;
using PMS.Domain.Constants;
using PMS.Domain.Entities;
using Task = System.Threading.Tasks.Task;

namespace PMS.Application.Services.Implementations;

public class TimesheetService : BaseService<TimesheetService>, ITimesheetService
{
    private readonly IApplicationDbContext _context;
    private readonly IAccessScope _scope;

    public TimesheetService(ILogger<TimesheetService> logger, IApplicationDbContext context, IAccessScope scope) : base(logger)
    {
        _context = context;
        _scope = scope;
    }

    public async Task<TimesheetResponse> CreateTimesheetAsync(CreateTimesheetRequest request, string actorId)
    {
        return await ExecuteAsync(async () =>
        {
            var timesheet = new Timesheet
            {
                Name = request.Name,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                ProjectId = request.ProjectId,
                UserEmail = request.UserEmail,
                BillingType = request.BillingType,
            };
            _context.Timesheets.Add(timesheet);
            await _context.SaveChangesAsync();
            return MapToResponse(timesheet);
        }, nameof(CreateTimesheetAsync));
    }

    public async Task<IEnumerable<TimesheetResponse>> GetTimesheetsAsync(int? projectId, string? userEmail)
    {
        return await ExecuteAsync(async () =>
        {
            var queryable = _context.Timesheets.ScopeTimesheets(_scope).AsQueryable();
            if (projectId.HasValue) queryable = queryable.Where(ts => ts.ProjectId == projectId);
            if (!string.IsNullOrWhiteSpace(userEmail)) queryable = queryable.Where(ts => ts.UserEmail == userEmail);
            var items = await queryable.OrderByDescending(ts => ts.StartDate).ToListAsync();
            return items.Select(MapToResponse);
        }, nameof(GetTimesheetsAsync));
    }

    public async Task<TimesheetResponse?> GetTimesheetByIdAsync(int timesheetId)
    {
        return await ExecuteAsync(async () =>
        {
            var timesheet = await _context.Timesheets.ScopeTimesheets(_scope).FirstOrDefaultAsync(ts => ts.Id == timesheetId);
            return timesheet == null ? null : MapToResponse(timesheet);
        }, nameof(GetTimesheetByIdAsync));
    }

    public async Task<TimesheetResponse?> UpdateTimesheetAsync(int timesheetId, TimesheetUpdateRequest request, string actorId)
    {
        return await ExecuteAsync(async () =>
        {
            var timesheet = await _context.Timesheets.ScopeTimesheets(_scope).FirstOrDefaultAsync(ts => ts.Id == timesheetId);
            if (timesheet == null) return null;

            if (request.Name != null) timesheet.Name = request.Name;
            if (request.StartDate.HasValue) timesheet.StartDate = request.StartDate.Value;
            if (request.EndDate.HasValue) timesheet.EndDate = request.EndDate.Value;
            if (request.BillingType != null) timesheet.BillingType = request.BillingType;
            if (request.ApprovalStatus != null) timesheet.ApprovalStatus = request.ApprovalStatus;

            await _context.SaveChangesAsync();
            return MapToResponse(timesheet);
        }, nameof(UpdateTimesheetAsync));
    }

    public async Task<bool> DeleteTimesheetAsync(int timesheetId, string actorId)
    {
        return await ExecuteAsync(async () =>
        {
            var timesheet = await _context.Timesheets.ScopeTimesheets(_scope).FirstOrDefaultAsync(ts => ts.Id == timesheetId);
            if (timesheet == null) return false;
            timesheet.IsDeleted = true;
            await _context.SaveChangesAsync();
            return true;
        }, nameof(DeleteTimesheetAsync));
    }

    private static TimesheetResponse MapToResponse(Timesheet ts) => new(
        ts.Id, ts.Name, ts.StartDate, ts.EndDate, ts.ProjectId, ts.UserEmail,
        ts.BillingType, ts.TotalHours, ts.ApprovalStatus, ts.CreatedAt, ts.UpdatedAt
    );
}

public class DocumentService : BaseService<DocumentService>, IDocumentService
{
    private readonly IApplicationDbContext _context;
    private readonly IAccessScope _scope;

    public DocumentService(ILogger<DocumentService> logger, IApplicationDbContext context, IAccessScope scope) : base(logger)
    {
        _context = context;
        _scope = scope;
    }

    public async Task<DocumentResponse> CreateDocumentAsync(CreateDocumentRequest request, string actorId)
    {
        return await ExecuteAsync(async () =>
        {
            await _context.EnsureProjectAccessAsync(_scope, request.ProjectId);
            var publicId = "DOC-" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();
            var document = new Document
            {
                PublicId = publicId,
                Title = request.Title,
                Description = request.Description,
                FileUrl = request.FileUrl,
                FileType = request.FileType,
                FileSize = request.FileSize,
                ProjectId = request.ProjectId,
                UploadedByEmail = request.UploadedByEmail,
            };
            _context.Documents.Add(document);
            await _context.SaveChangesAsync();
            return MapToResponse(document);
        }, nameof(CreateDocumentAsync));
    }

    public async Task<IEnumerable<DocumentResponse>> GetDocumentsAsync(int? projectId)
    {
        return await ExecuteAsync(async () =>
        {
            var queryable = _context.Documents.ScopeDocuments(_scope).AsQueryable();
            if (projectId.HasValue) queryable = queryable.Where(d => d.ProjectId == projectId);
            var items = await queryable.OrderByDescending(d => d.Id).ToListAsync();
            return items.Select(MapToResponse);
        }, nameof(GetDocumentsAsync));
    }

    public async Task<DocumentResponse?> GetDocumentByIdAsync(int documentId)
    {
        return await ExecuteAsync(async () =>
        {
            var document = await _context.Documents.ScopeDocuments(_scope).FirstOrDefaultAsync(d => d.Id == documentId);
            return document == null ? null : MapToResponse(document);
        }, nameof(GetDocumentByIdAsync));
    }

    public async Task<DocumentResponse?> UpdateDocumentAsync(int documentId, DocumentUpdateRequest request, string actorId)
    {
        return await ExecuteAsync(async () =>
        {
            var document = await _context.Documents.ScopeDocuments(_scope).FirstOrDefaultAsync(d => d.Id == documentId);
            if (document == null) return null;

            if (request.Title != null) document.Title = request.Title;
            if (request.Description != null) document.Description = request.Description;

            await _context.SaveChangesAsync();
            return MapToResponse(document);
        }, nameof(UpdateDocumentAsync));
    }

    public async Task<bool> DeleteDocumentAsync(int documentId, string actorId)
    {
        return await ExecuteAsync(async () =>
        {
            var document = await _context.Documents.ScopeDocuments(_scope).FirstOrDefaultAsync(d => d.Id == documentId);
            if (document == null) return false;
            document.IsDeleted = true;
            await _context.SaveChangesAsync();
            return true;
        }, nameof(DeleteDocumentAsync));
    }

    private static DocumentResponse MapToResponse(Document d) => new(
        d.Id, d.PublicId, d.Title, d.Description, d.FileUrl, d.FileType, d.FileSize,
        d.ProjectId, d.UploadedByEmail, d.CreatedAt, d.UpdatedAt
    );
}

public class ProjectGroupService : BaseService<ProjectGroupService>, IProjectGroupService
{
    private readonly IApplicationDbContext _context;

    public ProjectGroupService(ILogger<ProjectGroupService> logger, IApplicationDbContext context) : base(logger)
    {
        _context = context;
    }

    public async Task<ProjectGroupResponse> CreateProjectGroupAsync(CreateProjectGroupRequest request, string actorId)
    {
        return await ExecuteAsync(async () =>
        {
            var group = new ProjectGroup { Name = request.Name, Description = request.Description };
            _context.ProjectGroups.Add(group);
            await _context.SaveChangesAsync();
            return MapToResponse(group);
        }, nameof(CreateProjectGroupAsync));
    }

    public async Task<IEnumerable<ProjectGroupResponse>> GetProjectGroupsAsync()
    {
        return await ExecuteAsync(async () =>
        {
            var items = await _context.ProjectGroups.OrderBy(g => g.Name).ToListAsync();
            return items.Select(MapToResponse);
        }, nameof(GetProjectGroupsAsync));
    }

    public async Task<ProjectGroupResponse?> GetProjectGroupByIdAsync(int projectGroupId)
    {
        return await ExecuteAsync(async () =>
        {
            var group = await _context.ProjectGroups.FirstOrDefaultAsync(g => g.Id == projectGroupId);
            return group == null ? null : MapToResponse(group);
        }, nameof(GetProjectGroupByIdAsync));
    }

    public async Task<ProjectGroupResponse?> UpdateProjectGroupAsync(int projectGroupId, ProjectGroupUpdateRequest request, string actorId)
    {
        return await ExecuteAsync(async () =>
        {
            var group = await _context.ProjectGroups.FirstOrDefaultAsync(g => g.Id == projectGroupId);
            if (group == null) return null;

            if (request.Name != null) group.Name = request.Name;
            if (request.Description != null) group.Description = request.Description;

            await _context.SaveChangesAsync();
            return MapToResponse(group);
        }, nameof(UpdateProjectGroupAsync));
    }

    public async Task<bool> DeleteProjectGroupAsync(int projectGroupId, string actorId)
    {
        return await ExecuteAsync(async () =>
        {
            var group = await _context.ProjectGroups.FirstOrDefaultAsync(g => g.Id == projectGroupId);
            if (group == null) return false;
            group.IsDeleted = true;
            await _context.SaveChangesAsync();
            return true;
        }, nameof(DeleteProjectGroupAsync));
    }

    private static ProjectGroupResponse MapToResponse(ProjectGroup g) => new(
        g.Id, g.Name, g.Description, g.CreatedAt, g.UpdatedAt
    );
}
