using PMS.Domain.Common;

namespace PMS.Domain.Entities;

/// <summary>A typed URL stored against a project (repo, live site, staging, design, docs...).</summary>
public class ProjectLink : AuditableEntity
{
    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    public required string Title { get; set; }
    public required string Url { get; set; }
    public required string LinkType { get; set; }
    public string? Description { get; set; }

    /// <summary>False = everyone who can open the project. True = creator, Admins and the shared roles/users.</summary>
    public bool RestrictAccess { get; set; }

    public int CreatedByUserId { get; set; }
    public User CreatedByUser { get; set; } = null!;

    public ICollection<ProjectLinkRoleAccess> RoleAccess { get; set; } = new List<ProjectLinkRoleAccess>();
    public ICollection<ProjectLinkUserAccess> UserAccess { get; set; } = new List<ProjectLinkUserAccess>();
}

public class ProjectLinkRoleAccess
{
    public int LinkId { get; set; }
    public ProjectLink Link { get; set; } = null!;
    public int RoleId { get; set; }
    public Role Role { get; set; } = null!;
}

public class ProjectLinkUserAccess
{
    public int LinkId { get; set; }
    public ProjectLink Link { get; set; } = null!;
    public int UserId { get; set; }
    public User User { get; set; } = null!;
}

/// <summary>A free-text note on a project. Only its author or an Admin can edit or delete it.</summary>
public class ProjectNote : AuditableEntity
{
    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    public string? Title { get; set; }
    public required string Body { get; set; }
    public bool IsPinned { get; set; }

    public bool RestrictAccess { get; set; }

    public int CreatedByUserId { get; set; }
    public User CreatedByUser { get; set; } = null!;

    public ICollection<ProjectNoteRoleAccess> RoleAccess { get; set; } = new List<ProjectNoteRoleAccess>();
    public ICollection<ProjectNoteUserAccess> UserAccess { get; set; } = new List<ProjectNoteUserAccess>();
}

public class ProjectNoteRoleAccess
{
    public int NoteId { get; set; }
    public ProjectNote Note { get; set; } = null!;
    public int RoleId { get; set; }
    public Role Role { get; set; } = null!;
}

public class ProjectNoteUserAccess
{
    public int NoteId { get; set; }
    public ProjectNote Note { get; set; } = null!;
    public int UserId { get; set; }
    public User User { get; set; } = null!;
}
