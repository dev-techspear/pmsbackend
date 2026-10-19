using PMS.Domain.Common;

namespace PMS.Domain.Entities;

public class ProjectTemplate : AuditableEntity
{
    public string? PublicId { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public string? BillingType { get; set; }
    public bool IsPublic { get; set; } = true;

    public int? SourceProjectId { get; set; }
    public Project? SourceProject { get; set; }

    public int? CreatedById { get; set; }
    public User? CreatedBy { get; set; }

    public ICollection<TemplateTask> Tasks { get; set; } = new List<TemplateTask>();
}
