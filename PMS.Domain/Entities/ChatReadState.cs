using PMS.Domain.Common;

namespace PMS.Domain.Entities;

/// <summary>How far a user has read in a project's chat room (drives unread counts).</summary>
public class ChatReadState : BaseEntity
{
    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int LastReadMessageId { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
