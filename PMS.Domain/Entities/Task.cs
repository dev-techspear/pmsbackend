using PMS.Domain.Common;

namespace PMS.Domain.Entities;

public class Task : AuditableEntity
{
    public required string PublicId { get; set; }
    public required string TaskName { get; set; }
    public string? Description { get; set; }

    public int? ProjectId { get; set; }
    public Project? Project { get; set; }

    public int? TaskListId { get; set; }
    public TaskList? TaskList { get; set; }

    public int? MilestoneId { get; set; }
    public Milestone? Milestone { get; set; }

    public int? AssociatedTeamId { get; set; }
    public Team? AssociatedTeam { get; set; }

    public int? AssigneeId { get; set; }
    public User? Assignee { get; set; }

    public int? CreatedById { get; set; }
    public User? Creator { get; set; }

    public int? OwnerId { get; set; }
    public User? SingleOwner { get; set; }

    public int? StatusId { get; set; }
    public MasterLookup? StatusMaster { get; set; }

    public int? PriorityId { get; set; }
    public MasterLookup? PriorityMaster { get; set; }

    public string? Tags { get; set; }

    public DateTime? StartDate { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? CompletionDate { get; set; }

    public int? Duration { get; set; }
    public int CompletionPercentage { get; set; } = 0;

    public decimal? EstimatedHours { get; set; }
    public decimal? WorkHours { get; set; }
    public decimal CachedTimelogTotal { get; set; } = 0.0m;
    public string BillingType { get; set; } = "Billable";

    public bool IsProcessed { get; set; } = false;
    public int? PreviousStatusId { get; set; }
    public MasterLookup? PreviousStatusMaster { get; set; }

    public ICollection<User> Owners { get; set; } = new List<User>();
    public ICollection<User> Assignees { get; set; } = new List<User>();
    public ICollection<TimeLog> Timelogs { get; set; } = new List<TimeLog>();
}
