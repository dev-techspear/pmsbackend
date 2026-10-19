using PMS.Domain.Common;

namespace PMS.Domain.Entities;

public class TaskList : AuditableEntity
{
    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    public int? MilestoneId { get; set; }
    public Milestone? Milestone { get; set; }

    public required string Name { get; set; }
    public string? Description { get; set; }

    public ICollection<Task> Tasks { get; set; } = new List<Task>();
}
