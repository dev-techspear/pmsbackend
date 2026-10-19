using PMS.Domain.Common;

namespace PMS.Domain.Entities;

public class ProjectGroup : AuditableEntity
{
    public required string Name { get; set; }
    public string? Description { get; set; }
}
