using PMS.Domain.Common;

namespace PMS.Domain.Entities;

// Deliberately NOT an AuditableEntity — the source `template_tasks` table has no
// is_active/is_deleted/created_at/updated_at columns (see SOURCE_DATABASE_SCHEMA.md).
public class TemplateTask : BaseEntity
{
    public int TemplateId { get; set; }
    public ProjectTemplate Template { get; set; } = null!;

    public required string Title { get; set; }
    public string? Description { get; set; }
    public decimal? EstimatedHours { get; set; }
    public int? Duration { get; set; }
    public string? BillingType { get; set; }
    public string? Tags { get; set; }
    public int OrderIndex { get; set; } = 0;
}
