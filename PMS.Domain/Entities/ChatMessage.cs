using PMS.Domain.Common;

namespace PMS.Domain.Entities;

/// <summary>One message in a project's chat room (a project has exactly one room).</summary>
public class ChatMessage : AuditableEntity
{
    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    // Null once the sender's account is removed; the message itself is kept.
    public int? SenderId { get; set; }
    public User? Sender { get; set; }

    public required string Body { get; set; }
    public DateTime? EditedAt { get; set; }
}
