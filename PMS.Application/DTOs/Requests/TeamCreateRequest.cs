using System.Collections.Generic;

namespace PMS.Application.DTOs.Requests;

public record TeamCreateRequest(
    string Name,
    string TeamEmail,
    decimal BudgetAllocation,
    string? Description,
    string? TeamType,
    int? MaxTeamSize,
    string? PrimaryCommunicationChannel,
    string? ChannelId,
    string? LeadEmail,
    IEnumerable<string>? MemberEmails
);
