using PMS.Domain.Common;

namespace PMS.Domain.Entities;

public class Skill : AuditableEntity
{
    public required string Name { get; set; }

    public ICollection<User> Users { get; set; } = new List<User>();
}
