namespace PMS.Application.Common.Interfaces;

/// <summary>
/// One transactional e-mail. When <see cref="TemplateKey"/> is set the mail service renders its own
/// template with <see cref="TemplateParams"/>; <see cref="Subject"/>, <see cref="Html"/> and <see cref="Text"/>
/// are then only the fallback used if that template is missing, inactive or not allowed for the profile.
/// </summary>
public record OutgoingMail(
    string ToEmail,
    string? ToName,
    string Subject,
    string Html,
    string Text,
    string IdempotencyKey,
    string? CorrelationType = null,
    string? CorrelationId = null,
    string? TemplateKey = null,
    IReadOnlyDictionary<string, string>? TemplateParams = null);

/// <summary>Sends transactional e-mail through the Techspear Mail Service.</summary>
public interface IMailSender
{
    /// <summary>False when no mail service is set up for this environment (nothing can be delivered).</summary>
    bool IsConfigured { get; }

    /// <summary>True when the mail service accepted the message. Never throws for delivery problems.</summary>
    Task<bool> SendAsync(OutgoingMail mail, CancellationToken ct = default);
}
