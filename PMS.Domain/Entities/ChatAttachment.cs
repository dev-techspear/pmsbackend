using PMS.Domain.Common;

namespace PMS.Domain.Entities;

/// <summary>
/// A file attached to a chat message. The bytes live in Azure Blob Storage (Cool tier); this
/// row is metadata plus <see cref="BlobPath"/>, the blob's path within the chat-attachments
/// container. Access is still gated by the room's access rules - a SAS URL is only minted
/// after that check passes (see ChatService.GetAttachmentAsync).
/// </summary>
public class ChatAttachment : BaseEntity
{
    public int MessageId { get; set; }
    public ChatMessage Message { get; set; } = null!;

    public required string FileName { get; set; }
    public required string ContentType { get; set; }
    public int Size { get; set; }
    public required string BlobPath { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
