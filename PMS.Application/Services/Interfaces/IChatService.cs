using PMS.Application.DTOs;

namespace PMS.Application.Services.Interfaces;

public interface IChatService
{
    /// <summary>Admin, or someone tied to the project (owner, manager, delivery head, member) - and for
    /// external users, whose role also grants chat-view.</summary>
    Task<bool> CanAccessAsync(int projectId, int userId);

    /// <summary>Newest-last page of messages; pass <paramref name="beforeId"/> to load older ones.</summary>
    Task<IReadOnlyList<ChatMessageDto>> GetMessagesAsync(int projectId, int userId, int? beforeId, int limit);

    /// <summary>People in the room who can be @mentioned (owner, manager, delivery head, members).</summary>
    Task<IReadOnlyList<ChatMemberDto>> GetMembersAsync(int projectId, int userId);

    /// <summary>Post a message, optionally with files (validated: type, size, count, real file signature).</summary>
    Task<ChatMessageDto> PostAsync(int projectId, int userId, string body, IReadOnlyList<ChatUpload>? files = null);

    /// <summary>A file from this room, or null if it does not exist / belongs to a deleted message.</summary>
    Task<ChatDownload?> GetAttachmentAsync(int projectId, int attachmentId, int userId);

    /// <summary>Own messages only. Returns null when the message does not exist.</summary>
    Task<ChatMessageDto?> EditAsync(int projectId, int messageId, int userId, string body);

    /// <summary>Own messages (admins may delete any). Returns false when the message does not exist.</summary>
    Task<bool> DeleteAsync(int projectId, int messageId, int userId);

    Task MarkReadAsync(int projectId, int userId, int lastMessageId);

    /// <summary>Unread counts for every project room the user can enter (others' messages only).</summary>
    Task<IReadOnlyList<ChatUnreadDto>> GetUnreadAsync(int userId);
}
