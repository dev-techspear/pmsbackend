namespace PMS.Domain.Entities;

public class ProjectMember
{
    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public string? ProjectProfile { get; set; }
    public string? PortalProfile { get; set; }
    public string? RoleInProject { get; set; }

    public int? InvitationStatusId { get; set; }
    public MasterLookup? InvitationStatusMaster { get; set; }

    public bool IsOwner { get; set; } = false;
    public bool IsProcessed { get; set; } = false;

    public int? PreviousInvitationStatusId { get; set; }
    public MasterLookup? PreviousInvitationStatusMaster { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
