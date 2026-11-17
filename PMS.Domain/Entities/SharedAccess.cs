using PMS.Domain.Common;

namespace PMS.Domain.Entities;

/// <summary>A role that may open a restricted document (SharePoint-style "shared with").</summary>
public class DocumentRoleAccess
{
    public int DocumentId { get; set; }
    public Document Document { get; set; } = null!;

    public int RoleId { get; set; }
    public Role Role { get; set; } = null!;
}

/// <summary>A single user a restricted document was shared with directly.</summary>
public class DocumentUserAccess
{
    public int DocumentId { get; set; }
    public Document Document { get; set; } = null!;

    public int UserId { get; set; }
    public User User { get; set; } = null!;
}

/// <summary>
/// A secret stored against a project (server login, DB string, API key...). The secret is only
/// ever held encrypted (<see cref="SecretEncrypted"/>); list responses never carry it and it is
/// returned only by the audited "reveal" call. Sharing works like documents: when
/// <see cref="RestrictAccess"/> is set only the creator, Admins, the shared roles and the shared
/// users can see it at all.
/// </summary>
public class ProjectCredential : AuditableEntity
{
    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    public required string Label { get; set; }
    public required string CredentialType { get; set; }
    public string? Host { get; set; }
    public string? Username { get; set; }
    public required string SecretEncrypted { get; set; }
    public string? Notes { get; set; }
    public DateTime? LastRotatedAt { get; set; }

    public bool RestrictAccess { get; set; } = true;

    public int CreatedByUserId { get; set; }
    public User CreatedByUser { get; set; } = null!;

    public ICollection<ProjectCredentialRoleAccess> RoleAccess { get; set; } = new List<ProjectCredentialRoleAccess>();
    public ICollection<ProjectCredentialUserAccess> UserAccess { get; set; } = new List<ProjectCredentialUserAccess>();
}

public class ProjectCredentialRoleAccess
{
    public int CredentialId { get; set; }
    public ProjectCredential Credential { get; set; } = null!;

    public int RoleId { get; set; }
    public Role Role { get; set; } = null!;
}

public class ProjectCredentialUserAccess
{
    public int CredentialId { get; set; }
    public ProjectCredential Credential { get; set; } = null!;

    public int UserId { get; set; }
    public User User { get; set; } = null!;
}

/// <summary>Append-only trail of who revealed / changed a credential.</summary>
public class ProjectCredentialAccessLog : BaseEntity
{
    public int CredentialId { get; set; }
    public int UserId { get; set; }
    public required string Action { get; set; }
    public DateTime At { get; set; } = DateTime.UtcNow;
}
