namespace PMS.Application.DTOs;

public record ChatAttachmentDto(int Id, string FileName, string ContentType, int Size);

public record ChatMessageDto(
    int Id,
    int ProjectId,
    int? SenderId,
    string SenderName,
    bool SenderIsExternal,
    string Body,
    DateTime CreatedAt,
    DateTime? EditedAt,
    IReadOnlyList<ChatAttachmentDto> Attachments
);

public record ChatUnreadDto(int ProjectId, int Count, int MentionCount);

/// <summary>One row of the global chat inbox's conversation list.</summary>
public record ChatRoomDto(
    int ProjectId,
    string ProjectName,
    string? LastMessageBody,
    string? LastMessageSenderName,
    DateTime? LastMessageAt,
    bool LastMessageHasAttachments,
    int UnreadCount,
    int MentionCount
);

/// <summary>Someone who can be @mentioned in a room.</summary>
public record ChatMemberDto(int Id, string Name, bool IsExternal);

public record PostChatMessageRequest(string Body);

public record MarkChatReadRequest(int LastMessageId);

/// <summary>A file received with a message, already read into memory (size-checked by the caller).</summary>
public record ChatUpload(string FileName, string ContentType, byte[] Content);

/// <summary>A short-lived, read-only SAS URL pointing straight at the blob - the API never
/// streams attachment bytes itself.</summary>
public record ChatDownload(string FileName, string ContentType, string Url);

public record ChatTicketDto(string Ticket, int ExpiresInSeconds);
