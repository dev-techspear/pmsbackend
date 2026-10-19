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

public class MilestoneService : BaseService<MilestoneService>, IMilestoneService
{
    private readonly IApplicationDbContext _context;
    private readonly IAccessScope _scope;

    public MilestoneService(ILogger<MilestoneService> logger, IApplicationDbContext context, IAccessScope scope) : base(logger)
    {
        _context = context;
        _scope = scope;
    }

    private IQueryable<Milestone> IncludeGraph(IQueryable<Milestone> query, bool scoped = true) =>
        (scoped ? query.ScopeMilestones(_scope) : query)
        .Include(m => m.Owner)
        .Include(m => m.StatusMaster)
        .Include(m => m.PriorityMaster);

    public async Task<MilestoneResponse> CreateMilestoneAsync(CreateMilestoneRequest request, string actorId)
    {
        return await ExecuteAsync(async () =>
        {
            await _context.EnsureProjectAccessAsync(_scope, request.ProjectId);
            var publicId = "MS-" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();
            var milestone = new Milestone
            {
                PublicId = publicId,
                MilestoneName = request.MilestoneName,
                ProjectId = request.ProjectId,
                OwnerId = request.OwnerId,
                StatusId = request.StatusId,
                PriorityId = request.PriorityId,
                Description = request.Description,
                Flags = request.Flags,
                Tags = request.Tags,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
            };
            _context.Milestones.Add(milestone);
            await _context.SaveChangesAsync();

            var reloaded = await IncludeGraph(_context.Milestones, scoped: false).FirstAsync(m => m.Id == milestone.Id);
            return MapToMilestoneResponse(reloaded);
        }, nameof(CreateMilestoneAsync));
    }

    public async Task<MilestonePagedResponseDto> GetMilestonesAsync(int skip, int limit, int? projectId, int? statusId)
    {
        return await ExecuteAsync(async () =>
        {
            var queryable = IncludeGraph(_context.Milestones).AsQueryable();
            if (projectId.HasValue) queryable = queryable.Where(m => m.ProjectId == projectId);
            if (statusId.HasValue) queryable = queryable.Where(m => m.StatusId == statusId);

            var total = await queryable.CountAsync();
            var items = await queryable.OrderByDescending(m => m.Id).Skip(skip).Take(limit).ToListAsync();
            return new MilestonePagedResponseDto(total, items.Select(MapToMilestoneResponse));
        }, nameof(GetMilestonesAsync));
    }

    public async Task<IEnumerable<MilestoneResponse>> SearchMilestonesAsync(string query, int limit)
    {
        return await ExecuteAsync(async () =>
        {
            var queryable = IncludeGraph(_context.Milestones).AsQueryable();
            if (!string.IsNullOrWhiteSpace(query))
            {
                var term = query.ToLower();
                queryable = queryable.Where(m => m.MilestoneName.ToLower().Contains(term));
            }
            var items = await queryable.OrderBy(m => m.MilestoneName).Take(limit).ToListAsync();
            return items.Select(MapToMilestoneResponse);
        }, nameof(SearchMilestonesAsync));
    }

    public async Task<MilestoneResponse?> GetMilestoneByIdAsync(int milestoneId)
    {
        return await ExecuteAsync(async () =>
        {
            var milestone = await IncludeGraph(_context.Milestones).FirstOrDefaultAsync(m => m.Id == milestoneId);
            return milestone == null ? null : MapToMilestoneResponse(milestone);
        }, nameof(GetMilestoneByIdAsync));
    }

    public async Task<MilestoneResponse?> UpdateMilestoneAsync(int milestoneId, MilestoneUpdateRequest request, string actorId)
    {
        return await ExecuteAsync(async () =>
        {
            var milestone = await IncludeGraph(_context.Milestones).FirstOrDefaultAsync(m => m.Id == milestoneId);
            if (milestone == null) return null;

            if (request.MilestoneName != null) milestone.MilestoneName = request.MilestoneName;
            if (request.OwnerId.HasValue) milestone.OwnerId = request.OwnerId;
            if (request.StatusId.HasValue)
            {
                milestone.PreviousStatusId = milestone.StatusId;
                milestone.StatusId = request.StatusId;
            }
            if (request.PriorityId.HasValue) milestone.PriorityId = request.PriorityId;
            if (request.Description != null) milestone.Description = request.Description;
            if (request.Flags != null) milestone.Flags = request.Flags;
            if (request.Tags != null) milestone.Tags = request.Tags;
            if (request.StartDate.HasValue) milestone.StartDate = request.StartDate;
            if (request.EndDate.HasValue) milestone.EndDate = request.EndDate;
            if (request.CompletionPercentage.HasValue) milestone.CompletionPercentage = request.CompletionPercentage.Value;

            await _context.SaveChangesAsync();
            return MapToMilestoneResponse(milestone);
        }, nameof(UpdateMilestoneAsync));
    }

    public async Task<bool> DeleteMilestoneAsync(int milestoneId, string actorId)
    {
        return await ExecuteAsync(async () =>
        {
            var milestone = await _context.Milestones.ScopeMilestones(_scope).FirstOrDefaultAsync(m => m.Id == milestoneId);
            if (milestone == null) return false;
            milestone.IsDeleted = true;
            await _context.SaveChangesAsync();
            return true;
        }, nameof(DeleteMilestoneAsync));
    }

    private static MasterLookupResponse? MapMasterLookup(MasterLookup? lookup) => lookup == null
        ? null
        : new MasterLookupResponse(lookup.Id, lookup.Category, lookup.Value, lookup.Label, lookup.Color, lookup.Icon, lookup.OrderIndex, lookup.IsActive);

    private static UserResponseBaseDto? MapUserBase(User? user) => user == null
        ? null
        : new UserResponseBaseDto(user.Id, user.PublicId, user.Email, user.Username, user.FirstName, user.LastName, user.DisplayName, user.IsExternal, user.IsSynced);

    private static MilestoneResponse MapToMilestoneResponse(Milestone m) => new(
        m.Id, m.PublicId, m.MilestoneName, m.Description, m.ProjectId,
        m.OwnerId, MapUserBase(m.Owner),
        m.StatusId, m.PriorityId, MapMasterLookup(m.StatusMaster), MapMasterLookup(m.PriorityMaster),
        m.Flags, m.Tags, m.StartDate, m.EndDate, m.CompletionPercentage,
        m.CreatedAt, m.UpdatedAt
    );
}
