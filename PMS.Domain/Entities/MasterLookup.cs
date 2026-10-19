using PMS.Domain.Common;

namespace PMS.Domain.Entities;

public class MasterLookup : AuditableEntity
{
    public required string Category { get; set; }
    public required string Value { get; set; }
    public required string Label { get; set; }
    public string? Color { get; set; }
    public string? Icon { get; set; }
    public int OrderIndex { get; set; } = 0;
}
