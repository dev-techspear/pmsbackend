using PMS.Domain.Common;

namespace PMS.Domain.Entities;

public class Role : AuditableEntity
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public string Permissions { get; set; } = "{}"; // Stored as JSON string

    public ICollection<User> Users { get; set; } = new List<User>();
}
