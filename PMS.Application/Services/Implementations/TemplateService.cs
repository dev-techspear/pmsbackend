using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PMS.Application.Common;
using PMS.Application.Common.Interfaces;
using PMS.Application.DTOs.Requests;
using PMS.Application.DTOs.Responses;
using PMS.Application.Services.Interfaces;
using PMS.Domain.Entities;
using Task = System.Threading.Tasks.Task;

namespace PMS.Application.Services.Implementations;

public class ProjectTemplateService : BaseService<ProjectTemplateService>, IProjectTemplateService
{
    private readonly IApplicationDbContext _context;

    public ProjectTemplateService(ILogger<ProjectTemplateService> logger, IApplicationDbContext context) : base(logger)
    {
        _context = context;
    }

    private static IQueryable<ProjectTemplate> IncludeGraph(IQueryable<ProjectTemplate> query) => query
        .Include(pt => pt.Tasks.OrderBy(t => t.OrderIndex));

    public async Task<ProjectTemplateResponse> CreateTemplateAsync(CreateProjectTemplateRequest request, string actorId)
    {
        return await ExecuteAsync(async () =>
        {
            int? createdById = int.TryParse(actorId, out var aid) ? aid : null;
            var publicId = "TPL-" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();

            var template = new ProjectTemplate
            {
                PublicId = publicId,
                Name = request.Name,
                Description = request.Description,
                BillingType = request.BillingType,
                IsPublic = request.IsPublic,
                SourceProjectId = request.SourceProjectId,
                CreatedById = createdById,
            };
            _context.ProjectTemplates.Add(template);
            await _context.SaveChangesAsync();
            return MapToResponse(template);
        }, nameof(CreateTemplateAsync));
    }

    public async Task<ProjectTemplateListResponseDto> GetTemplatesAsync(int skip, int limit)
    {
        return await ExecuteAsync(async () =>
        {
            var queryable = IncludeGraph(_context.ProjectTemplates).AsQueryable();
            var total = await queryable.CountAsync();
            var items = await queryable.OrderBy(pt => pt.Name).Skip(skip).Take(limit).ToListAsync();
            return new ProjectTemplateListResponseDto(total, items.Select(MapToResponse));
        }, nameof(GetTemplatesAsync));
    }

    public async Task<ProjectTemplateResponse?> GetTemplateByIdAsync(int templateId)
    {
        return await ExecuteAsync(async () =>
        {
            var template = await IncludeGraph(_context.ProjectTemplates).FirstOrDefaultAsync(pt => pt.Id == templateId);
            return template == null ? null : MapToResponse(template);
        }, nameof(GetTemplateByIdAsync));
    }

    public async Task<ProjectTemplateResponse?> UpdateTemplateAsync(int templateId, ProjectTemplateUpdateRequest request, string actorId)
    {
        return await ExecuteAsync(async () =>
        {
            var template = await IncludeGraph(_context.ProjectTemplates).FirstOrDefaultAsync(pt => pt.Id == templateId);
            if (template == null) return null;

            if (request.Name != null) template.Name = request.Name;
            if (request.Description != null) template.Description = request.Description;
            if (request.BillingType != null) template.BillingType = request.BillingType;
            if (request.IsPublic.HasValue) template.IsPublic = request.IsPublic.Value;

            await _context.SaveChangesAsync();
            return MapToResponse(template);
        }, nameof(UpdateTemplateAsync));
    }

    public async Task<bool> DeleteTemplateAsync(int templateId, string actorId)
    {
        return await ExecuteAsync(async () =>
        {
            var template = await _context.ProjectTemplates.FirstOrDefaultAsync(pt => pt.Id == templateId);
            if (template == null) return false;
            template.IsDeleted = true;
            await _context.SaveChangesAsync();
            return true;
        }, nameof(DeleteTemplateAsync));
    }

    public async Task<TemplateTaskResponse?> AddTaskAsync(int templateId, CreateTemplateTaskRequest request)
    {
        return await ExecuteAsync(async () =>
        {
            var template = await _context.ProjectTemplates.FirstOrDefaultAsync(pt => pt.Id == templateId);
            if (template == null) return null;

            var task = new TemplateTask
            {
                TemplateId = templateId,
                Title = request.Title,
                Description = request.Description,
                EstimatedHours = request.EstimatedHours,
                Duration = request.Duration,
                BillingType = request.BillingType,
                Tags = request.Tags,
                OrderIndex = request.OrderIndex,
            };
            _context.TemplateTasks.Add(task);
            await _context.SaveChangesAsync();
            return MapTaskToResponse(task);
        }, nameof(AddTaskAsync));
    }

    public async Task<bool> RemoveTaskAsync(int templateId, int taskId)
    {
        return await ExecuteAsync(async () =>
        {
            var task = await _context.TemplateTasks.FirstOrDefaultAsync(t => t.Id == taskId && t.TemplateId == templateId);
            if (task == null) return false;
            _context.TemplateTasks.Remove(task);
            await _context.SaveChangesAsync();
            return true;
        }, nameof(RemoveTaskAsync));
    }

    private static TemplateTaskResponse MapTaskToResponse(TemplateTask t) => new(
        t.Id, t.TemplateId, t.Title, t.Description, t.EstimatedHours, t.Duration, t.BillingType, t.Tags, t.OrderIndex
    );

    private static ProjectTemplateResponse MapToResponse(ProjectTemplate pt) => new(
        pt.Id, pt.PublicId, pt.Name, pt.Description, pt.BillingType, pt.IsPublic,
        pt.SourceProjectId, pt.CreatedById,
        pt.Tasks?.Select(MapTaskToResponse) ?? Enumerable.Empty<TemplateTaskResponse>(),
        pt.CreatedAt, pt.UpdatedAt
    );
}

public class AuditService : BaseService<AuditService>, IAuditService
{
    private readonly IApplicationDbContext _context;

    public AuditService(ILogger<AuditService> logger, IApplicationDbContext context) : base(logger)
    {
        _context = context;
    }

    public async Task<IEnumerable<AuditLogResponse>> GetAuditLogsAsync(int skip, int limit, string? tableName)
    {
        return await ExecuteAsync(async () =>
        {
            var queryable = _context.AuditLogs.AsQueryable();
            if (!string.IsNullOrWhiteSpace(tableName))
            {
                queryable = queryable.Where(a => a.TableName == tableName);
            }
            var items = await queryable
                .OrderByDescending(a => a.PerformedOn)
                .Skip(skip).Take(limit)
                .ToListAsync();

            return items.Select(a => new AuditLogResponse(
                a.ID, a.TableName, a.Action, a.PerformedBy, a.PerformedOn,
                a.RecordName, a.TransactionId, a.Comments, a.ModuleName
            ));
        }, nameof(GetAuditLogsAsync));
    }
}
