using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PMS.Application.Common;
using PMS.Application.Common.Interfaces;
using PMS.Application.DTOs;
using PMS.Application.Services.Interfaces;
using PMS.Domain.Constants;
using PMS.Domain.Entities;
using Task = System.Threading.Tasks.Task;

namespace PMS.Application.Services.Implementations;

public class ChatService : BaseService<ChatService>, IChatService
{
    private readonly IApplicationDbContext _context;
    private readonly IAccessScope _scope;

    public ChatService(ILogger<ChatService> logger, IApplicationDbContext context, IAccessScope scope) : base(logger)
    {
        _context = context;
        _scope = scope;
    }

    private async Task EnsureScopeAsync(int userId)
    {
        if (!_scope.IsLoaded || _scope.UserId != userId) await _scope.LoadAsync(userId);
    }

    // Membership defined once so room access, unread counts and the hub all agree.
    private static IQueryable<Project> TiedTo(IQueryable<Project> q, int uid) =>
        q.Where(p => p.OwnerId == uid || p.ProjectManagerId == uid || p.DeliveryHeadId == uid
                     || p.TeamMembers.Any(m => m.UserId == uid));

    private bool RoleAllows(string permissionId)
    {
        // External users get nothing the role does not grant; internal users are not gated by role here.
        return !_scope.IsExternal || _scope.IsAdmin || _scope.Level(permissionId) != null;
    }

    public async Task<bool> CanAccessAsync(int projectId, int userId)
    {
        await EnsureScopeAsync(userId);
        if (!RoleAllows(AppConstants.PermChatView)) return false;
        if (_scope.IsAdmin) return await _context.Projects.AnyAsync(p => p.Id == projectId);
        return await TiedTo(_context.Projects, userId).AnyAsync(p => p.Id == projectId);
    }

    private async Task RequireAccessAsync(int projectId, int userId, bool posting = false)
    {
        if (!await CanAccessAsync(projectId, userId))
            throw new AccessDeniedException("You do not have access to this project's chat.");
        if (posting && !RoleAllows(AppConstants.PermChatPost))
            throw new AccessDeniedException("You do not have permission to post in chat.");
    }

    private static string DisplayNameOf(User u) => u.DisplayName ?? $"{u.FirstName} {u.LastName}".Trim();

    // ---------- members / mentions ----------

    private async Task<List<User>> LoadRoomPeopleAsync(int projectId)
    {
        var project = await _context.Projects
            .Include(p => p.TeamMembers).ThenInclude(m => m.User)
            .Include(p => p.Owner).Include(p => p.ProjectManager).Include(p => p.DeliveryHead)
            .FirstAsync(p => p.Id == projectId);

        return new[] { project.Owner, project.ProjectManager, project.DeliveryHead }
            .Concat(project.TeamMembers.Select(m => m.User))
            .Where(u => u != null && u.IsActive && !u.IsDeleted)
            .Select(u => u!)
            .DistinctBy(u => u.Id)
            .OrderBy(u => u.DisplayName ?? u.FirstName)
            .ToList();
    }

    public async Task<IReadOnlyList<ChatMemberDto>> GetMembersAsync(int projectId, int userId)
    {
        return await ExecuteAsync(async () =>
        {
            await RequireAccessAsync(projectId, userId);
            var people = await LoadRoomPeopleAsync(projectId);
            return (IReadOnlyList<ChatMemberDto>)people
                .Select(u => new ChatMemberDto(u.Id, DisplayNameOf(u), u.IsExternal)).ToList();
        }, nameof(GetMembersAsync));
    }

    /// <summary>Validate the client's mention tokens against the room and return the cleaned body + who was mentioned.</summary>
    private async Task<(string Body, IReadOnlyList<int> Ids)> NormalizeMentionsAsync(int projectId, string body)
    {
        if (!body.Contains("@[")) return (body, Array.Empty<int>());
        var people = await LoadRoomPeopleAsync(projectId);
        return ChatMentions.Normalize(body, people.ToDictionary(u => u.Id, DisplayNameOf));
    }

    // ---------- mapping ----------

    private static ChatMessageDto ToDto(ChatMessage m, User? sender, IReadOnlyList<ChatAttachmentDto> attachments) => new(
        m.Id,
        m.ProjectId,
        m.SenderId,
        sender == null ? "Deleted user" : DisplayNameOf(sender),
        sender?.IsExternal ?? false,
        m.Body,
        DateTime.SpecifyKind(m.CreatedAt, DateTimeKind.Utc),
        m.EditedAt.HasValue ? DateTime.SpecifyKind(m.EditedAt.Value, DateTimeKind.Utc) : null,
        attachments);

    /// <summary>Attachment metadata for a set of messages, without ever reading the file bytes.</summary>
    private async Task<ILookup<int, ChatAttachmentDto>> AttachmentsForAsync(IEnumerable<int> messageIds)
    {
        var ids = messageIds.ToList();
        var rows = await _context.ChatAttachments
            .Where(a => ids.Contains(a.MessageId))
            .OrderBy(a => a.Id)
            .Select(a => new { a.MessageId, Dto = new ChatAttachmentDto(a.Id, a.FileName, a.ContentType, a.Size) })
            .ToListAsync();
        return rows.ToLookup(r => r.MessageId, r => r.Dto);
    }

    private static string CleanBody(string? body, bool allowEmpty)
    {
        var text = (body ?? "").Trim();
        if (text.Length == 0 && !allowEmpty) throw new InvalidOperationException("Message cannot be empty.");
        if (text.Length > AppConstants.ChatMaxBodyLength)
            throw new InvalidOperationException($"Message must be {AppConstants.ChatMaxBodyLength} characters or fewer.");
        return text;
    }

    // ---------- attachments ----------

    private static readonly Dictionary<string, string> MimeByExtension = new(StringComparer.OrdinalIgnoreCase)
    {
        [".png"] = "image/png", [".jpg"] = "image/jpeg", [".jpeg"] = "image/jpeg", [".gif"] = "image/gif",
        [".webp"] = "image/webp", [".pdf"] = "application/pdf", [".txt"] = "text/plain", [".csv"] = "text/csv",
        [".md"] = "text/markdown", [".doc"] = "application/msword",
        [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        [".xls"] = "application/vnd.ms-excel",
        [".xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        [".ppt"] = "application/vnd.ms-powerpoint",
        [".pptx"] = "application/vnd.openxmlformats-officedocument.presentationml.presentation",
        [".zip"] = "application/zip",
    };

    /// <summary>The first bytes of a file must match what its extension claims (a renamed .exe is refused).</summary>
    internal static bool SignatureMatches(string extension, byte[] b)
    {
        static bool Starts(byte[] data, params byte[] sig) =>
            data.Length >= sig.Length && sig.Select((v, i) => data[i] == v).All(x => x);

        return extension.ToLowerInvariant() switch
        {
            ".png" => Starts(b, 0x89, 0x50, 0x4E, 0x47),
            ".jpg" or ".jpeg" => Starts(b, 0xFF, 0xD8, 0xFF),
            ".gif" => Starts(b, 0x47, 0x49, 0x46, 0x38),
            ".webp" => Starts(b, 0x52, 0x49, 0x46, 0x46) && b.Length > 12 && Starts(b[8..], 0x57, 0x45, 0x42, 0x50),
            ".pdf" => Starts(b, 0x25, 0x50, 0x44, 0x46),
            ".zip" or ".docx" or ".xlsx" or ".pptx" => Starts(b, 0x50, 0x4B),
            ".doc" or ".xls" or ".ppt" => Starts(b, 0xD0, 0xCF, 0x11, 0xE0),
            _ => !b.Contains((byte)0),   // plain text formats: no binary content
        };
    }

    internal static string CleanFileName(string name)
    {
        var leaf = Path.GetFileName(name.Replace('\\', '/'));
        var cleaned = new string(leaf.Where(c => !char.IsControl(c) && c != '"').ToArray()).Trim();
        if (cleaned.Length > 255) cleaned = cleaned[^255..];
        return cleaned;
    }

    private static List<(string Name, string Mime, byte[] Content)> ValidateUploads(IReadOnlyList<ChatUpload>? files)
    {
        var result = new List<(string, string, byte[])>();
        if (files == null || files.Count == 0) return result;
        if (files.Count > AppConstants.ChatMaxAttachments)
            throw new InvalidOperationException($"You can attach at most {AppConstants.ChatMaxAttachments} files to a message.");

        foreach (var f in files)
        {
            var name = CleanFileName(f.FileName ?? "");
            var ext = Path.GetExtension(name);
            if (name.Length == 0 || string.IsNullOrEmpty(ext) || !MimeByExtension.TryGetValue(ext, out var mime))
                throw new InvalidOperationException(
                    $"\"{name}\" is not an allowed file type ({string.Join(", ", AppConstants.ChatAllowedExtensions)}).");
            if (f.Content.Length == 0) throw new InvalidOperationException($"\"{name}\" is empty.");
            if (f.Content.Length > AppConstants.ChatMaxAttachmentBytes)
                throw new InvalidOperationException(
                    $"\"{name}\" is larger than {AppConstants.ChatMaxAttachmentBytes / (1024 * 1024)} MB.");
            if (!SignatureMatches(ext, f.Content))
                throw new InvalidOperationException($"\"{name}\" does not look like a real {ext} file.");
            // The MIME type comes from the extension we validated, never from what the client claimed.
            result.Add((name, mime, f.Content));
        }
        return result;
    }

    public async Task<ChatDownload?> GetAttachmentAsync(int projectId, int attachmentId, int userId)
    {
        return await ExecuteAsync(async () =>
        {
            await RequireAccessAsync(projectId, userId);
            var file = await _context.ChatAttachments
                .Where(a => a.Id == attachmentId && a.Message.ProjectId == projectId && !a.Message.IsDeleted)
                .Select(a => new ChatDownload(a.FileName, a.ContentType, a.Content))
                .FirstOrDefaultAsync();
            return file;
        }, nameof(GetAttachmentAsync));
    }

    // ---------- messages ----------

    public async Task<IReadOnlyList<ChatMessageDto>> GetMessagesAsync(int projectId, int userId, int? beforeId, int limit)
    {
        return await ExecuteAsync(async () =>
        {
            await RequireAccessAsync(projectId, userId);
            limit = Math.Clamp(limit, 1, AppConstants.ChatMaxPageSize);

            var query = _context.ChatMessages.Include(m => m.Sender).Where(m => m.ProjectId == projectId);
            if (beforeId.HasValue) query = query.Where(m => m.Id < beforeId.Value);

            var page = await query.OrderByDescending(m => m.Id).Take(limit).ToListAsync();
            page.Reverse();
            var attachments = await AttachmentsForAsync(page.Select(m => m.Id));
            return (IReadOnlyList<ChatMessageDto>)page
                .Select(m => ToDto(m, m.Sender, attachments[m.Id].ToList())).ToList();
        }, nameof(GetMessagesAsync));
    }

    public async Task<ChatMessageDto> PostAsync(int projectId, int userId, string body, IReadOnlyList<ChatUpload>? files = null)
    {
        return await ExecuteAsync(async () =>
        {
            await RequireAccessAsync(projectId, userId, posting: true);
            var uploads = ValidateUploads(files);
            var text = CleanBody(body, allowEmpty: uploads.Count > 0);
            var (normalized, mentionedIds) = await NormalizeMentionsAsync(projectId, text);

            var message = new ChatMessage { ProjectId = projectId, SenderId = userId, Body = normalized };
            _context.ChatMessages.Add(message);
            foreach (var (name, mime, content) in uploads)
            {
                _context.ChatAttachments.Add(new ChatAttachment
                {
                    Message = message, FileName = name, ContentType = mime, Size = content.Length, Content = content,
                });
            }
            foreach (var id in mentionedIds) _context.ChatMentions.Add(new ChatMention { Message = message, UserId = id });
            await _context.SaveChangesAsync();

            var sender = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            var attachments = (await AttachmentsForAsync(new[] { message.Id }))[message.Id].ToList();
            return ToDto(message, sender, attachments);
        }, nameof(PostAsync));
    }

    public async Task<ChatMessageDto?> EditAsync(int projectId, int messageId, int userId, string body)
    {
        return await ExecuteAsync(async () =>
        {
            await RequireAccessAsync(projectId, userId, posting: true);

            var message = await _context.ChatMessages.Include(m => m.Sender)
                .FirstOrDefaultAsync(m => m.Id == messageId && m.ProjectId == projectId);
            if (message == null) return null;
            if (message.SenderId != userId) throw new AccessDeniedException("You can only edit your own messages.");

            var attachments = (await AttachmentsForAsync(new[] { message.Id }))[message.Id].ToList();
            var text = CleanBody(body, allowEmpty: attachments.Count > 0);
            var (normalized, mentionedIds) = await NormalizeMentionsAsync(projectId, text);

            message.Body = normalized;
            message.EditedAt = DateTime.UtcNow;

            // Re-derive who is mentioned from the new text.
            _context.ChatMentions.RemoveRange(_context.ChatMentions.Where(x => x.MessageId == message.Id));
            foreach (var id in mentionedIds) _context.ChatMentions.Add(new ChatMention { MessageId = message.Id, UserId = id });
            await _context.SaveChangesAsync();
            return ToDto(message, message.Sender, attachments);
        }, nameof(EditAsync));
    }

    public async Task<bool> DeleteAsync(int projectId, int messageId, int userId)
    {
        return await ExecuteAsync(async () =>
        {
            await RequireAccessAsync(projectId, userId, posting: true);

            var message = await _context.ChatMessages
                .FirstOrDefaultAsync(m => m.Id == messageId && m.ProjectId == projectId);
            if (message == null) return false;
            if (message.SenderId != userId && !_scope.IsAdmin)
                throw new AccessDeniedException("You can only delete your own messages.");

            message.IsDeleted = true;   // soft delete; the row (and its files) stay for audit
            await _context.SaveChangesAsync();
            return true;
        }, nameof(DeleteAsync));
    }

    public async Task MarkReadAsync(int projectId, int userId, int lastMessageId)
    {
        await ExecuteAsync(async () =>
        {
            await RequireAccessAsync(projectId, userId);

            // A client cannot mark messages that do not exist yet as read.
            var newest = await _context.ChatMessages.Where(m => m.ProjectId == projectId).MaxAsync(m => (int?)m.Id) ?? 0;
            lastMessageId = Math.Min(lastMessageId, newest);

            var state = await _context.ChatReadStates
                .FirstOrDefaultAsync(r => r.ProjectId == projectId && r.UserId == userId);
            if (state == null)
            {
                _context.ChatReadStates.Add(new ChatReadState
                {
                    ProjectId = projectId, UserId = userId, LastReadMessageId = lastMessageId,
                });
            }
            else if (lastMessageId > state.LastReadMessageId)   // never move the marker backwards
            {
                state.LastReadMessageId = lastMessageId;
                state.UpdatedAt = DateTime.UtcNow;
            }
            await _context.SaveChangesAsync();
            return true;
        }, nameof(MarkReadAsync));
    }

    /// <summary>Unread messages per room for a user. Public so a test can prove it translates to SQL Server.</summary>
    public IQueryable<ChatUnreadDto> BuildUnreadQuery(int userId, bool isAdmin)
    {
        // Rooms the user can enter: tied projects (admins: every project).
        IQueryable<Project> rooms = isAdmin ? _context.Projects : TiedTo(_context.Projects, userId);

        return
            from m in _context.ChatMessages
            join r in rooms on m.ProjectId equals r.Id
            join s in _context.ChatReadStates.Where(x => x.UserId == userId)
                on m.ProjectId equals s.ProjectId into reads
            from s in reads.DefaultIfEmpty()
            where m.SenderId != userId && m.Id > (s == null ? 0 : s.LastReadMessageId)
            group m by m.ProjectId into g
            select new ChatUnreadDto(
                g.Key,
                g.Count(),
                // Only mentions the server validated when the message was saved.
                g.Count(x => _context.ChatMentions.Any(cm => cm.MessageId == x.Id && cm.UserId == userId)));
    }

    public async Task<IReadOnlyList<ChatUnreadDto>> GetUnreadAsync(int userId)
    {
        return await ExecuteAsync(async () =>
        {
            await EnsureScopeAsync(userId);
            if (!RoleAllows(AppConstants.PermChatView)) return (IReadOnlyList<ChatUnreadDto>)new List<ChatUnreadDto>();

            var counts = await BuildUnreadQuery(userId, _scope.IsAdmin).ToListAsync();
            return (IReadOnlyList<ChatUnreadDto>)counts;
        }, nameof(GetUnreadAsync));
    }
}
