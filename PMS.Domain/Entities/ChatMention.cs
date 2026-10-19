using PMS.Domain.Common;

namespace PMS.Domain.Entities;

/// <summary>
/// A validated @mention: the server records it only when the mentioned person really is in the room,
/// so counts cannot be inflated by typing a token by hand.
/// </summary>
public class ChatMention : BaseEntity
{
    public int MessageId { get; set; }
    public ChatMessage Message { get; set; } = null!;

    public int UserId { get; set; }
    public User User { get; set; } = null!;
}
