using PMS.Domain.Common;

namespace PMS.Domain.Entities;

public class Milestone : AuditableEntity
{
    public required string PublicId { get; set; }
    public required string MilestoneName { get; set; }
    public string? Description { get; set; }

    public int? ProjectId { get; set; }
    public Project? Project { get; set; }

    public int? OwnerId { get; set; }
    public User? Owner { get; set; }

    public int? StatusId { get; set; }
    public MasterLookup? StatusMaster { get; set; }

    public int? PriorityId { get; set; }
    public MasterLookup? PriorityMaster { get; set; }

    public string? Flags { get; set; }
    public string? Tags { get; set; }

    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    public int CompletionPercentage { get; set; } = 0;
    public bool IsProcessed { get; set; } = false;

    public int? PreviousStatusId { get; set; }
    public MasterLookup? PreviousStatusMaster { get; set; }

    public ICollection<TaskList> TaskLists { get; set; } = new List<TaskList>();
    public ICollection<Issue> Issues { get; set; } = new List<Issue>();
}

