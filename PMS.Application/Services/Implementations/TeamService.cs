using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PMS.Application.Common;
using PMS.Application.Common.Interfaces;
using PMS.Application.DTOs.Requests;
using PMS.Application.DTOs.Responses;
using PMS.Application.Services.Interfaces;
using PMS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using Task = System.Threading.Tasks.Task;

namespace PMS.Application.Services.Implementations;

public class TeamService : BaseService<TeamService>, ITeamService
{
    private readonly IApplicationDbContext _context;

    public TeamService(ILogger<TeamService> logger, IApplicationDbContext context) : base(logger)
    {
        _context = context;
    }

    public async Task<TeamResponseDto> CreateTeamAsync(TeamCreateRequest request, string actorId)
    {
        return await ExecuteAsync(async () =>
        {
            var publicId = "TM-" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();
            var team = new Team
            {
                PublicId = publicId,
                Name = request.Name,
                TeamEmail = request.TeamEmail,
                BudgetAllocation = request.BudgetAllocation,
                Description = request.Description,
                TeamType = request.TeamType,
                MaxTeamSize = request.MaxTeamSize,
                PrimaryCommunicationChannel = request.PrimaryCommunicationChannel,
                ChannelId = request.ChannelId,
                LeadEmail = request.LeadEmail
            };

            // Associate members if provided
            if (request.MemberEmails != null && request.MemberEmails.Any())
            {
                var members = await _context.Users
                    .Where(u => request.MemberEmails.Contains(u.Email))
                    .ToListAsync();
                foreach (var member in members)
                {
                    team.Members.Add(member);
                }
            }

            // Associate lead if lead email provided
            if (!string.IsNullOrEmpty(request.LeadEmail))
            {
                var lead = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.LeadEmail);
                if (lead != null)
                {
                    team.Lead = lead;
                }
            }

            _context.Teams.Add(team);
            await _context.SaveChangesAsync();

            // reload
            var reloadedTeam = await _context.Teams
                .Include(t => t.Members)
                .Include(t => t.Lead)
                .FirstAsync(t => t.Id == team.Id);

            return MapToTeamDto(reloadedTeam);
        }, nameof(CreateTeamAsync));
    }

    public async Task<TeamListResponseDto> GetTeamsAsync(int skip, int limit)
    {
        return await ExecuteAsync(async () =>
        {
            var queryable = _context.Teams
                .Include(t => t.Members)
                .Include(t => t.Lead)
                .AsQueryable();

            var total = await queryable.CountAsync();
            var items = await queryable
                .OrderBy(t => t.Id)
                .Skip(skip)
                .Take(limit)
                .ToListAsync();

            var dtos = items.Select(MapToTeamDto);
            return new TeamListResponseDto(total, dtos);
        }, nameof(GetTeamsAsync));
    }

    public async Task<IEnumerable<TeamResponseDto>> SearchTeamsAsync(string query, int limit)
    {
        return await ExecuteAsync(async () =>
        {
            var queryable = _context.Teams
                .Include(t => t.Members)
                .Include(t => t.Lead)
                .AsQueryable();

            if (!string.IsNullOrEmpty(query))
            {
                var queryLower = query.ToLower();
                queryable = queryable.Where(t => t.Name.ToLower().Contains(queryLower));
            }

            var items = await queryable
                .OrderBy(t => t.Id)
                .Take(limit)
                .ToListAsync();

            return items.Select(MapToTeamDto);
        }, nameof(SearchTeamsAsync));
    }

    public async Task<TeamWithMembersResponseDto?> GetTeamByIdAsync(int teamId)
    {
        return await ExecuteAsync(async () =>
        {
            var team = await _context.Teams
                .Include(t => t.Members)
                .Include(t => t.Lead)
                .FirstOrDefaultAsync(t => t.Id == teamId);

            if (team == null) return null;
            return MapToTeamWithMembersDto(team);
        }, nameof(GetTeamByIdAsync));
    }

    public async Task<TeamResponseDto?> UpdateTeamAsync(int teamId, TeamUpdateRequest request, string actorId)
    {
        return await ExecuteAsync(async () =>
        {
            var team = await _context.Teams
                .Include(t => t.Members)
                .Include(t => t.Lead)
                .FirstOrDefaultAsync(t => t.Id == teamId);

            if (team == null) return null;

            if (request.Name != null) team.Name = request.Name;
            if (request.TeamEmail != null) team.TeamEmail = request.TeamEmail;
            if (request.BudgetAllocation.HasValue) team.BudgetAllocation = request.BudgetAllocation.Value;
            if (request.Description != null) team.Description = request.Description;
            if (request.TeamType != null) team.TeamType = request.TeamType;
            if (request.MaxTeamSize.HasValue) team.MaxTeamSize = request.MaxTeamSize;
            if (request.PrimaryCommunicationChannel != null) team.PrimaryCommunicationChannel = request.PrimaryCommunicationChannel;
            if (request.ChannelId != null) team.ChannelId = request.ChannelId;
            if (request.LeadEmail != null) team.LeadEmail = request.LeadEmail;

            // Associate lead
            if (request.LeadEmail != null)
            {
                if (string.IsNullOrEmpty(request.LeadEmail))
                {
                    team.Lead = null;
                }
                else
                {
                    var lead = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.LeadEmail);
                    if (lead != null) team.Lead = lead;
                }
            }

            // Sync members
            if (request.MemberEmails != null)
            {
                team.Members.Clear();
                var members = await _context.Users
                    .Where(u => request.MemberEmails.Contains(u.Email))
                    .ToListAsync();
                foreach (var member in members)
                {
                    team.Members.Add(member);
                }
            }

            await _context.SaveChangesAsync();
            return MapToTeamDto(team);
        }, nameof(UpdateTeamAsync));
    }

    public async Task<bool> DeleteTeamAsync(int teamId, string actorId)
    {
        return await ExecuteAsync(async () =>
        {
            var team = await _context.Teams.FirstOrDefaultAsync(t => t.Id == teamId);
            if (team == null) return false;

            _context.Teams.Remove(team);
            await _context.SaveChangesAsync();
            return true;
        }, nameof(DeleteTeamAsync));
    }

    public async Task<bool> AddTeamMemberAsync(int teamId, string userEmail, string actorId)
    {
        return await ExecuteAsync(async () =>
        {
            var team = await _context.Teams
                .Include(t => t.Members)
                .FirstOrDefaultAsync(t => t.Id == teamId);

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);

            if (team == null || user == null) return false;

            if (!team.Members.Any(m => m.Id == user.Id))
            {
                team.Members.Add(user);
                await _context.SaveChangesAsync();
                return true;
            }

            return false;
        }, nameof(AddTeamMemberAsync));
    }

    public async Task<bool> RemoveTeamMemberAsync(int teamId, string userEmail, string actorId)
    {
        return await ExecuteAsync(async () =>
        {
            var team = await _context.Teams
                .Include(t => t.Members)
                .FirstOrDefaultAsync(t => t.Id == teamId);

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);

            if (team == null || user == null) return false;

            var existingMember = team.Members.FirstOrDefault(m => m.Id == user.Id);
            if (existingMember != null)
            {
                team.Members.Remove(existingMember);
                await _context.SaveChangesAsync();
                return true;
            }

            return false;
        }, nameof(RemoveTeamMemberAsync));
    }

    private TeamResponseDto MapToTeamDto(Team team)
    {
        UserResponseBaseDto? leadDto = null;
        if (team.Lead != null)
        {
            leadDto = new UserResponseBaseDto(
                team.Lead.Id,
                team.Lead.PublicId,
                team.Lead.Email,
                team.Lead.Username,
                team.Lead.FirstName,
                team.Lead.LastName,
                team.Lead.DisplayName,
                team.Lead.IsExternal,
                team.Lead.IsSynced
            );
        }

        return new TeamResponseDto(
            team.Id,
            team.PublicId,
            team.Name,
            team.TeamEmail,
            team.BudgetAllocation,
            team.Description,
            team.TeamType,
            team.MaxTeamSize,
            team.PrimaryCommunicationChannel,
            team.ChannelId,
            team.LeadEmail,
            leadDto,
            team.Members?.Count ?? 0
        );
    }

    private TeamWithMembersResponseDto MapToTeamWithMembersDto(Team team)
    {
        UserResponseBaseDto? leadDto = null;
        if (team.Lead != null)
        {
            leadDto = new UserResponseBaseDto(
                team.Lead.Id,
                team.Lead.PublicId,
                team.Lead.Email,
                team.Lead.Username,
                team.Lead.FirstName,
                team.Lead.LastName,
                team.Lead.DisplayName,
                team.Lead.IsExternal,
                team.Lead.IsSynced
            );
        }

        var membersList = team.Members?.Select(m => new UserResponseDto(
            m.Id,
            m.PublicId,
            m.EmployeeId,
            m.FirstName,
            m.LastName,
            m.Email,
            m.Username,
            m.Phone,
            m.JobTitle,
            m.JoinDate,
            m.RoleId,
            m.StatusId,
            m.ManagerEmail,
            m.O365Id,
            m.DisplayName,
            m.Gender,
            m.Country,
            m.State,
            m.Language,
            m.Timezone,
            m.IsExternal,
            m.IsSynced,
            null,
            null,
            null,
            Enumerable.Empty<SkillResponse>()
        )) ?? Enumerable.Empty<UserResponseDto>();

        return new TeamWithMembersResponseDto(
            team.Id,
            team.PublicId,
            team.Name,
            team.TeamEmail,
            team.BudgetAllocation,
            team.Description,
            team.TeamType,
            team.MaxTeamSize,
            team.PrimaryCommunicationChannel,
            team.ChannelId,
            team.LeadEmail,
            leadDto,
            team.Members?.Count ?? 0,
            membersList
        );
    }
}
