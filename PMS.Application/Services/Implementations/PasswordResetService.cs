using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PMS.Application.Common;
using PMS.Application.Common.Interfaces;
using PMS.Application.Services.Interfaces;
using PMS.Domain.Constants;
using PMS.Domain.Entities;
using Task = System.Threading.Tasks.Task;

namespace PMS.Application.Services.Implementations;

/// <summary>
/// Forgot password for people who sign in with an e-mail and password (external users).
/// Microsoft SSO users reset through Microsoft, so they never get a link here.
/// Rules: the answer never reveals whether an e-mail has an account; the link is single-use,
/// expires in an hour, and a newer request cancels older links; at most a few requests an hour
/// per account; only a hash of the token is stored.
/// </summary>
public class PasswordResetService : BaseService<PasswordResetService>, IPasswordResetService
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _hasher;
    private readonly IMailSender _mail;
    private readonly PasswordResetSettings _settings;

    public PasswordResetService(ILogger<PasswordResetService> logger, IApplicationDbContext context,
        IPasswordHasher hasher, IMailSender mail, PasswordResetSettings settings) : base(logger)
    {
        _context = context;
        _hasher = hasher;
        _mail = mail;
        _settings = settings;
    }

    public async Task<ForgotPasswordOutcome> RequestAsync(string? email)
    {
        return await ExecuteAsync(async () =>
        {
            if (!_mail.IsConfigured || string.IsNullOrWhiteSpace(_settings.FrontendBaseUrl))
            {
                LogError(new InvalidOperationException("MailService or Frontend:BaseUrl is not configured"),
                    "A password reset was requested but the mail service or Frontend:BaseUrl is not configured.");
                return ForgotPasswordOutcome.Unavailable;
            }

            var normalized = (email ?? string.Empty).Trim().ToLowerInvariant();
            var user = normalized.Length == 0 ? null : await _context.Users.FirstOrDefaultAsync(u =>
                u.Email == normalized && u.IsExternal && u.PasswordHash != null && u.IsActive && !u.IsDeleted);
            if (user == null) return ForgotPasswordOutcome.Accepted;

            var now = DateTime.UtcNow;
            var recent = await _context.PasswordResetTokens
                .CountAsync(t => t.UserId == user.Id && t.CreatedAt > now.AddHours(-1));
            if (recent >= AppConstants.PasswordResetMaxRequestsPerHour)
            {
                LogWarn("Password reset throttled for user {UserId}", user.Id);
                return ForgotPasswordOutcome.Accepted;
            }

            // A newer request cancels every older link.
            var older = await _context.PasswordResetTokens.Where(t => t.UserId == user.Id && t.UsedAt == null).ToListAsync();
            foreach (var o in older) o.UsedAt = now;

            var raw = WebEncoders(RandomNumberGenerator.GetBytes(32));
            _context.PasswordResetTokens.Add(new PasswordResetToken
            {
                UserId = user.Id,
                TokenHash = Hash(raw),
                CreatedAt = now,
                ExpiresAt = now.AddMinutes(AppConstants.PasswordResetTokenMinutes),
            });
            await _context.SaveChangesAsync();

            var link = $"{_settings.FrontendBaseUrl.TrimEnd('/')}/reset-password?token={raw}";
            var name = string.IsNullOrWhiteSpace(user.FirstName) ? null : user.FirstName;
            var sent = await _mail.SendAsync(new OutgoingMail(
                user.Email, name, "Reset your PMS password", ResetHtml(name, link), ResetText(name, link),
                IdempotencyKey: "pms-pwreset-" + Guid.NewGuid().ToString("N"),
                CorrelationType: "password-reset", CorrelationId: user.Id.ToString()));
            if (!sent) LogError(new InvalidOperationException("mail not accepted"), "Password reset mail for user {UserId} was not accepted by the mail service.", user.Id);

            return ForgotPasswordOutcome.Accepted;
        }, nameof(RequestAsync));
    }

    public async Task ResetAsync(string? token, string? newPassword)
    {
        await ExecuteAsync(async () =>
        {
            var problems = PasswordPolicy.Validate(newPassword).ToList();
            if (problems.Count > 0) throw new InvalidOperationException(string.Join(" ", problems));

            var now = DateTime.UtcNow;
            var hash = Hash((token ?? string.Empty).Trim());
            var row = await _context.PasswordResetTokens.Include(t => t.User)
                .FirstOrDefaultAsync(t => t.TokenHash == hash && t.UsedAt == null && t.ExpiresAt > now);
            var user = row?.User;
            if (row == null || user == null || user.IsDeleted || !user.IsActive || !user.IsExternal)
                throw new InvalidOperationException(AppConstants.PasswordResetInvalidLinkMessage);

            user.PasswordHash = _hasher.Hash(newPassword!);
            // Sign out everywhere: every token issued before this moment (access and refresh) is refused from now on.
            user.SessionsValidFrom = now;
            row.UsedAt = now;
            var others = await _context.PasswordResetTokens.Where(t => t.UserId == user.Id && t.UsedAt == null).ToListAsync();
            foreach (var o in others) o.UsedAt = now;
            await _context.SaveChangesAsync();

            // Best-effort heads-up so a takeover does not go unnoticed.
            await _mail.SendAsync(new OutgoingMail(
                user.Email, user.FirstName, "Your PMS password was changed",
                $"<p>Hi {WebUtility.HtmlEncode(user.FirstName)},</p><p>The password for your PMS account was just changed. If this was you, no action is needed. If it was not, contact support@techspear.in straight away.</p>",
                $"Hi {user.FirstName},\n\nThe password for your PMS account was just changed. If this was you, no action is needed. If it was not, contact support@techspear.in straight away.",
                IdempotencyKey: "pms-pwchanged-" + Guid.NewGuid().ToString("N"),
                CorrelationType: "password-changed", CorrelationId: user.Id.ToString()));
            return true;
        }, nameof(ResetAsync));
    }

    private static string Hash(string raw) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw))).ToLowerInvariant();

    // URL-safe base64 without padding: safe to put in a link.
    private static string WebEncoders(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static string ResetHtml(string? name, string link)
    {
        var greeting = string.IsNullOrWhiteSpace(name) ? "Hi," : $"Hi {WebUtility.HtmlEncode(name)},";
        var safeLink = WebUtility.HtmlEncode(link);
        return $"""
            <div style="font-family:Segoe UI,Arial,sans-serif;max-width:520px;margin:0 auto;color:#0f172a">
              <h2 style="margin:0 0 12px">Reset your PMS password</h2>
              <p>{greeting}</p>
              <p>We received a request to set a new password for your PMS account. Click the button to choose one. The link works once and expires in {AppConstants.PasswordResetTokenMinutes} minutes.</p>
              <p style="margin:24px 0"><a href="{safeLink}" style="background:#0cd1c3;color:#0f172a;text-decoration:none;font-weight:700;padding:12px 22px;border-radius:10px;display:inline-block">Set a new password</a></p>
              <p style="font-size:13px;color:#475569">If the button does not work, copy this address into your browser:<br>{safeLink}</p>
              <p style="font-size:13px;color:#475569">If you did not ask for this, you can ignore this email. Your password stays the same.</p>
            </div>
            """;
    }

    private static string ResetText(string? name, string link) =>
        $"{(string.IsNullOrWhiteSpace(name) ? "Hi," : $"Hi {name},")}\n\n" +
        $"We received a request to set a new password for your PMS account. Open this link to choose one (it works once and expires in {AppConstants.PasswordResetTokenMinutes} minutes):\n\n{link}\n\n" +
        "If you did not ask for this, ignore this email. Your password stays the same.";
}
