using PMS.Domain.Common;

namespace PMS.Domain.Entities;

public class Project : AuditableEntity
{
    public required string ProjectIdSync { get; set; }
    public required string PublicId { get; set; }

    public required string AccountName { get; set; }
    public required string ProjectName { get; set; }
    public required string CustomerName { get; set; }
    public string? ClientName { get; set; }

    public required string BillingModel { get; set; }
    public required string ProjectType { get; set; }
    public string? ProjectStatusExternal { get; set; }

    public DateTime? ExpectedStartDate { get; set; }
    public DateTime? ExpectedEndDate { get; set; }

    public int? OwnerId { get; set; }
    public User? Owner { get; set; }

    public int? ProjectManagerId { get; set; }
    public User? ProjectManager { get; set; }

    public int? DeliveryHeadId { get; set; }
    public User? DeliveryHead { get; set; }

    public int? TemplateId { get; set; }

    public int? StatusId { get; set; }
    public MasterLookup? StatusMaster { get; set; }

    public int? PriorityId { get; set; }
    public MasterLookup? PriorityMaster { get; set; }

    public string? Description { get; set; }
    public string? Tags { get; set; }

    public decimal EstimatedHours { get; set; } = 0.0m;
    public decimal ActualHours { get; set; } = 0.0m;

    public DateTime? ActualStartDate { get; set; }
    public DateTime? ActualEndDate { get; set; }

    public string? MsTeamsGroupId { get; set; }
    public string? MsTeamsChannelId { get; set; }

    public bool IsArchived { get; set; } = false;
    public bool IsTemplate { get; set; } = false;
    public bool IsGroup { get; set; } = false;
    public bool IsProcessed { get; set; } = false;

    public int? PreviousStatusId { get; set; }
    public MasterLookup? PreviousStatusMaster { get; set; }

    public ICollection<ProjectMember> TeamMembers { get; set; } = new List<ProjectMember>();
    public ICollection<Task> Tasks { get; set; } = new List<Task>();
    public ICollection<Issue> Issues { get; set; } = new List<Issue>();
    public ICollection<Milestone> Milestones { get; set; } = new List<Milestone>();
    public ICollection<TimeLog> Timelogs { get; set; } = new List<TimeLog>();
    public ICollection<TaskList> TaskLists { get; set; } = new List<TaskList>();
    public ICollection<Document> Documents { get; set; } = new List<Document>();
}
