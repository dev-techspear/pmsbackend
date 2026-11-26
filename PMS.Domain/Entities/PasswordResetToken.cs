using PMS.Domain.Common;

namespace PMS.Domain.Entities;

/// <summary>
/// A single-use "set a new password" link. Only the SHA-256 hash of the token is stored, so a
/// database leak cannot be turned into account takeovers; the raw token exists only in the e-mail.
/// </summary>
public class PasswordResetToken : BaseEntity
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public required string TokenHash { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }
}
