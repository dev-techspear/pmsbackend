using PMS.Domain.Common;

namespace PMS.Domain.Entities;

/// <summary>
/// A file attached to a chat message, stored with the message so it follows the room's access rules.
/// Never include this entity when listing messages - <see cref="Content"/> can be large.
/// </summary>
public class ChatAttachment : BaseEntity
{
    public int MessageId { get; set; }
    public ChatMessage Message { get; set; } = null!;

    public required string FileName { get; set; }
    public required string ContentType { get; set; }
    public int Size { get; set; }
    public required byte[] Content { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
