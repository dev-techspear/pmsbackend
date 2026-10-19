using PMS.Domain.Common;

namespace PMS.Domain.Entities;

public class User : AuditableEntity
{
    public required string PublicId { get; set; }
    public required string EmployeeId { get; set; }
    public required string FirstName { get; set; }
    public string? LastName { get; set; }
    public required string Email { get; set; }
    public required string Username { get; set; }
    public string? Phone { get; set; }
    public string? JobTitle { get; set; }
    public DateTime? JoinDate { get; set; }

    public string? PasswordHash { get; set; }

    public string? DisplayName { get; set; }
    public string? Gender { get; set; }
    public string? Country { get; set; }
    public string? State { get; set; }
    public string? Language { get; set; }
    public string? Timezone { get; set; }

    public string? O365Id { get; set; }
    public bool IsSynced { get; set; } = false;
    public bool IsExternal { get; set; } = false;

    public int? RoleId { get; set; }
    public Role? Role { get; set; }

    public int? StatusId { get; set; }
    public UserStatus? Status { get; set; }

    public string? ManagerEmail { get; set; }
    public User? Manager { get; set; }

    public ICollection<Team> Teams { get; set; } = new List<Team>();
    public ICollection<Skill> Skills { get; set; } = new List<Skill>();
    public ICollection<Team> ManagedTeams { get; set; } = new List<Team>();
    public ICollection<ProjectMember> ProjectMembers { get; set; } = new List<ProjectMember>();
}

