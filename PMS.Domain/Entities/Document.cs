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

    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    public string? UploadedByEmail { get; set; }
    public User? UploadedBy { get; set; }

    public ICollection<Issue> Issues { get; set; } = new List<Issue>();
}
