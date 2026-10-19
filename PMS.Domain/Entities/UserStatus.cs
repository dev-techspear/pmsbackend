using PMS.Domain.Common;

namespace PMS.Domain.Entities;

public class UserStatus : AuditableEntity
{
    public required string Name { get; set; }
}
