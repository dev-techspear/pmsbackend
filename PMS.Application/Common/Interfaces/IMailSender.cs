namespace PMS.Application.Common.Interfaces;

public record OutgoingMail(
    string ToEmail,
    string? ToName,
    string Subject,
    string Html,
    string Text,
    string IdempotencyKey,
    string? CorrelationType = null,
    string? CorrelationId = null);

/// <summary>Sends transactional e-mail through the Techspear Mail Service.</summary>
public interface IMailSender
{
    /// <summary>False when no mail service is set up for this environment (nothing can be delivered).</summary>
    bool IsConfigured { get; }

    /// <summary>True when the mail service accepted the message. Never throws for delivery problems.</summary>
    Task<bool> SendAsync(OutgoingMail mail, CancellationToken ct = default);
}
