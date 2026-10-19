using PMS.Domain.Common;

namespace PMS.Domain.Entities;

public class Team : AuditableEntity
{
    public required string PublicId { get; set; }
    public required string Name { get; set; }
    public required string TeamEmail { get; set; }
    public decimal BudgetAllocation { get; set; } = 0.00m;
    public string? Description { get; set; }
    public string? TeamType { get; set; }
    public int? MaxTeamSize { get; set; }
    public string? PrimaryCommunicationChannel { get; set; }
    public string? ChannelId { get; set; }

    public string? LeadEmail { get; set; }
    public User? Lead { get; set; }

    public ICollection<User> Members { get; set; } = new List<User>();
}
