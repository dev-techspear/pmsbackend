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

/// <summary>Someone who can be @mentioned in a room.</summary>
public record ChatMemberDto(int Id, string Name, bool IsExternal);

public record PostChatMessageRequest(string Body);

public record MarkChatReadRequest(int LastMessageId);

/// <summary>A file received with a message, already read into memory (size-checked by the caller).</summary>
public record ChatUpload(string FileName, string ContentType, byte[] Content);

public record ChatDownload(string FileName, string ContentType, byte[] Content);

public record ChatTicketDto(string Ticket, int ExpiresInSeconds);
