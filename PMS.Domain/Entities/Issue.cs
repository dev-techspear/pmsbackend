using PMS.Domain.Common;

namespace PMS.Domain.Entities;

public class Issue : AuditableEntity
{
    public required string PublicId { get; set; }
    public required string BugName { get; set; }
    public string? Description { get; set; }

    public int? ProjectId { get; set; }
    public Project? Project { get; set; }

    public int? MilestoneId { get; set; }
    public Milestone? Milestone { get; set; }

    public int? AssociatedTeamId { get; set; }
    public Team? AssociatedTeam { get; set; }

    public int? ReporterId { get; set; }
    public User? Reporter { get; set; }

    public int? AssigneeId { get; set; }
    public User? Assignee { get; set; }

    public int? StatusId { get; set; }
    public MasterLookup? StatusMaster { get; set; }

    public int? PriorityId { get; set; }
    public MasterLookup? PriorityMaster { get; set; }

    public int? SeverityId { get; set; }
    public MasterLookup? SeverityMaster { get; set; }

    public int? ClassificationId { get; set; }
    public MasterLookup? ClassificationMaster { get; set; }

    public string? Module { get; set; }
    public string? Tags { get; set; }
    public string? Flag { get; set; }

    public bool ReproducibleFlag { get; set; } = true;

    public DateTime? StartDate { get; set; }
    public DateTime? DueDate { get; set; }

    public DateTime? LastClosedTime { get; set; }
    public DateTime? LastModifiedTime { get; set; }

    public decimal? EstimatedHours { get; set; }
    public bool IsProcessed { get; set; } = false;

    public int? PreviousStatusId { get; set; }
    public MasterLookup? PreviousStatusMaster { get; set; }

    public ICollection<User> Followers { get; set; } = new List<User>();
    public ICollection<User> Assignees { get; set; } = new List<User>();
    public ICollection<Document> Documents { get; set; } = new List<Document>();
}
