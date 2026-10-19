using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.SignalR;
using PMS.API.Hubs;
using PMS.Application.Common.Interfaces;
using PMS.Application.DTOs;
using PMS.Application.Services.Interfaces;
using PMS.Domain.Constants;

namespace PMS.API.Controllers;

[Authorize]
[Route("api/v1")]
public class ChatController : BaseController
{
    // Room for 5 files of 10 MB plus the multipart overhead.
    private const long MaxRequestBytes = 60L * 1024 * 1024;

    private readonly IChatService _chat;
    private readonly IHubContext<ChatHub> _hub;
    private readonly ITokenService _tokens;

    public ChatController(IChatService chat, IHubContext<ChatHub> hub, ITokenService tokens)
    {
        _chat = chat;
        _hub = hub;
        _tokens = tokens;
    }

    private int Me => int.Parse(CurrentUserId);

    /// <summary>
    /// A one-minute credential for the chat WebSocket. Browsers can only send a WebSocket's token in
    /// the URL, and URLs get logged - so that token must be useless almost immediately.
    /// </summary>
    [HttpPost("chat/ticket")]
    public IActionResult Ticket()
        => OkResult(new ChatTicketDto(_tokens.GenerateChatTicket(Me), AppConstants.ChatTicketLifetimeSeconds));

    [HttpGet("projects/{projectId:int}/chat/messages")]
    public async Task<IActionResult> GetMessages(int projectId, [FromQuery] int? beforeId = null,
        [FromQuery] int limit = AppConstants.ChatDefaultPageSize)
        => OkResult(await _chat.GetMessagesAsync(projectId, Me, beforeId, limit));

    [HttpGet("projects/{projectId:int}/chat/members")]
    public async Task<IActionResult> GetMembers(int projectId)
        => OkResult(await _chat.GetMembersAsync(projectId, Me));

    [HttpPost("projects/{projectId:int}/chat/messages")]
    [Consumes("application/json")]
    [EnableRateLimiting(AppConstants.ChatRateLimitPolicy)]
    public Task<IActionResult> Post(int projectId, [FromBody] PostChatMessageRequest request)
        => PostCore(projectId, request.Body, null);

    /// <summary>Same as the JSON post, but multipart so files can ride along with the text.</summary>
    [HttpPost("projects/{projectId:int}/chat/messages")]
    [Consumes("multipart/form-data")]
    [EnableRateLimiting(AppConstants.ChatRateLimitPolicy)]
    [RequestSizeLimit(MaxRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxRequestBytes)]
    public async Task<IActionResult> PostWithFiles(int projectId, [FromForm] string? body, [FromForm] List<IFormFile>? files)
    {
        var uploads = new List<ChatUpload>();
        foreach (var file in files ?? [])
        {
            // Refuse oversized files from their declared length before reading a single byte.
            if (file.Length > AppConstants.ChatMaxAttachmentBytes)
                return BadRequestResult($"\"{Path.GetFileName(file.FileName)}\" is larger than {AppConstants.ChatMaxAttachmentBytes / (1024 * 1024)} MB.");

            using var ms = new MemoryStream((int)file.Length);
            await file.CopyToAsync(ms);
            uploads.Add(new ChatUpload(file.FileName, file.ContentType, ms.ToArray()));
        }
        return await PostCore(projectId, body ?? "", uploads);
    }

    private async Task<IActionResult> PostCore(int projectId, string body, IReadOnlyList<ChatUpload>? uploads)
    {
        try
        {
            var message = await _chat.PostAsync(projectId, Me, body, uploads);
            await _hub.Clients.Group(ChatHub.Group(projectId)).SendAsync("MessageReceived", message);
            return CreatedResult(message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequestResult(ex.Message);
        }
    }

    [HttpGet("projects/{projectId:int}/chat/attachments/{attachmentId:int}")]
    public async Task<IActionResult> Download(int projectId, int attachmentId)
    {
        var file = await _chat.GetAttachmentAsync(projectId, attachmentId, Me);
        if (file == null) return NotFoundResult("File not found.");

        // Never let the browser guess a type or render an uploaded file as a page.
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        Response.Headers["Cache-Control"] = "private, no-store";
        return File(file.Content, file.ContentType, file.FileName);
    }

    [HttpPut("projects/{projectId:int}/chat/messages/{messageId:int}")]
    [EnableRateLimiting(AppConstants.ChatRateLimitPolicy)]
    public async Task<IActionResult> Edit(int projectId, int messageId, [FromBody] PostChatMessageRequest request)
    {
        try
        {
            var message = await _chat.EditAsync(projectId, messageId, Me, request.Body);
            if (message == null) return NotFoundResult("Message not found.");
            await _hub.Clients.Group(ChatHub.Group(projectId)).SendAsync("MessageEdited", message);
            return OkResult(message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequestResult(ex.Message);
        }
    }

    [HttpDelete("projects/{projectId:int}/chat/messages/{messageId:int}")]
    public async Task<IActionResult> Delete(int projectId, int messageId)
    {
        if (!await _chat.DeleteAsync(projectId, messageId, Me)) return NotFoundResult("Message not found.");
        await _hub.Clients.Group(ChatHub.Group(projectId)).SendAsync("MessageDeleted", new { projectId, id = messageId });
        return NoContentResult();
    }

    [HttpPost("projects/{projectId:int}/chat/read")]
    public async Task<IActionResult> MarkRead(int projectId, [FromBody] MarkChatReadRequest request)
    {
        await _chat.MarkReadAsync(projectId, Me, request.LastMessageId);
        return NoContentResult();
    }

    [HttpGet("chat/unread")]
    public async Task<IActionResult> Unread() => OkResult(await _chat.GetUnreadAsync(Me));
}
