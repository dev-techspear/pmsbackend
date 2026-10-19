using System.Collections.Generic;

namespace PMS.Application.DTOs.Responses;

public record TeamWithMembersResponseDto(
    int Id,
    string PublicId,
    string Name,
    string TeamEmail,
    decimal BudgetAllocation,
    string? Description,
    string? TeamType,
    int? MaxTeamSize,
    string? PrimaryCommunicationChannel,
    string? ChannelId,
    string? LeadEmail,
    UserResponseBaseDto? Lead,
    int MembersCount,
    IEnumerable<UserResponseDto> Members
) : TeamResponseDto(Id, PublicId, Name, TeamEmail, BudgetAllocation, Description, TeamType, MaxTeamSize, PrimaryCommunicationChannel, ChannelId, LeadEmail, Lead, MembersCount);
