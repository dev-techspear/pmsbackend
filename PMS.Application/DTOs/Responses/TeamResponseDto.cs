namespace PMS.Application.DTOs.Responses;

public record TeamResponseDto(
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
    int MembersCount
);
