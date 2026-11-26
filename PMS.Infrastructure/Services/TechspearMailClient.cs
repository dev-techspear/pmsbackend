using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PMS.Application.Common.Interfaces;
using PMS.Domain.Constants;

namespace PMS.Infrastructure.Services;

/// <summary>
/// Client for the Techspear Mail Service (techspear-mail/docs/contracts.md): POST {BaseUrl}/messages
/// with "Authorization: Bearer keyId.secret", an Idempotency-Key and a raw subject + html body.
/// Settings: MailService:BaseUrl (ends in /v1), MailService:ApiKey, MailService:Profile (default noreply).
/// MailService:DevLogOnly=true (local development only) writes the mail to the log instead of sending.
/// </summary>
public sealed class TechspearMailClient : IMailSender
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    private readonly ILogger<TechspearMailClient> _logger;

    public TechspearMailClient(HttpClient http, IConfiguration config, ILogger<TechspearMailClient> logger)
    {
        _http = http;
        _config = config;
        _logger = logger;
        _http.Timeout = TimeSpan.FromSeconds(20); // the service is on a consumption plan: allow for cold starts
    }

    private bool DevLogOnly => string.Equals(_config["MailService:DevLogOnly"], "true", StringComparison.OrdinalIgnoreCase);
    private string? BaseUrl => _config["MailService:BaseUrl"];
    private string? ApiKey => _config["MailService:ApiKey"];

    public bool IsConfigured =>
        DevLogOnly || (Uri.TryCreate(BaseUrl, UriKind.Absolute, out _) && !IsPlaceholder(ApiKey));

    private static bool IsPlaceholder(string? v) => string.IsNullOrWhiteSpace(v) || v == "CHANGEME";

    public async Task<bool> SendAsync(OutgoingMail mail, CancellationToken ct = default)
    {
        if (DevLogOnly)
        {
            // Local development only: lets the whole flow be tried without a mail service.
            _logger.LogWarning("[DEV MAIL] to {To} | {Subject}\n{Text}", mail.ToEmail, mail.Subject, mail.Text);
            return true;
        }
        if (!IsConfigured) return false;

        var body = new
        {
            profile = _config["MailService:Profile"] is { Length: > 0 } p ? p : AppConstants.MailProfileDefault,
            to = new[] { new { email = mail.ToEmail, name = mail.ToName } },
            subject = mail.Subject,
            html = mail.Html,
            text = mail.Text,
            category = "transactional",
            correlation = mail.CorrelationType == null ? null : new { entityType = mail.CorrelationType, entityId = mail.CorrelationId },
        };

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, BaseUrl!.TrimEnd('/') + "/messages")
            {
                Content = new StringContent(JsonSerializer.Serialize(body, Json), Encoding.UTF8, "application/json"),
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ApiKey);
            request.Headers.Add("Idempotency-Key", mail.IdempotencyKey);

            using var response = await _http.SendAsync(request, ct);
            if ((int)response.StatusCode is 200 or 202) return true;

            var detail = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError("Mail service rejected a message: HTTP {Status} {Detail}", (int)response.StatusCode,
                detail.Length > 300 ? detail[..300] : detail);
            return false;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            _logger.LogError(ex, "Mail service could not be reached");
            return false;
        }
    }
}
