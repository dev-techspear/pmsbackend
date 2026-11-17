using PMS.Domain.Common;

namespace PMS.Domain.Entities;

public class Document : AuditableEntity
{
    public required string PublicId { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public required string FileUrl { get; set; }
    public string? FileType { get; set; }
    public int? FileSize { get; set; }

    /// <summary>Path of the uploaded file inside the blob container. Null for legacy link-only documents.</summary>
    public string? BlobPath { get; set; }

    /// <summary>
    /// False = every project viewer can open it (legacy behaviour). True = SharePoint-style:
    /// only the uploader, Admins, the shared roles and the shared users can see it.
    /// </summary>
    public bool RestrictAccess { get; set; }

    public ICollection<DocumentRoleAccess> RoleAccess { get; set; } = new List<DocumentRoleAccess>();
    public ICollection<DocumentUserAccess> UserAccess { get; set; } = new List<DocumentUserAccess>();

    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    public string? UploadedByEmail { get; set; }
    public User? UploadedBy { get; set; }

    public ICollection<Issue> Issues { get; set; } = new List<Issue>();
}
