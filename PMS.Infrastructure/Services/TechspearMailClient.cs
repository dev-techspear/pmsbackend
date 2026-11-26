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
/// with "Authorization: Bearer keyId.secret" and an Idempotency-Key.
/// A mail with a template key is sent as { template: { key, params } } so the wording lives in the mail
/// service. If the service rejects the template itself (unknown, inactive, or not allowed for the profile)
/// the mail is sent once more as a plain subject + html, so a template problem never loses a mail.
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
    private string Profile => _config["MailService:Profile"] is { Length: > 0 } p ? p : AppConstants.MailProfileDefault;

    public bool IsConfigured =>
        DevLogOnly || (Uri.TryCreate(BaseUrl, UriKind.Absolute, out _) && !IsPlaceholder(ApiKey));

    private static bool IsPlaceholder(string? v) => string.IsNullOrWhiteSpace(v) || v == "CHANGEME";

    private sealed record Result(bool Accepted, int Status, string Detail);

    public async Task<bool> SendAsync(OutgoingMail mail, CancellationToken ct = default)
    {
        if (DevLogOnly)
        {
            // Local development only: lets the whole flow be tried without a mail service.
            _logger.LogWarning("[DEV MAIL] to {To} | {Subject} | template {Template}\n{Text}",
                mail.ToEmail, mail.Subject, mail.TemplateKey ?? "(none)", mail.Text);
            return true;
        }
        if (!IsConfigured) return false;

        if (mail.TemplateKey == null)
            return Report(await PostAsync(RawBody(mail), mail.IdempotencyKey, ct));

        var first = await PostAsync(TemplateBody(mail), mail.IdempotencyKey, ct);
        if (first.Accepted) return true;

        // The service names the field it refused ("template.key"): that is a template problem, not a mail problem.
        var templateRefused = first.Status == 400 && first.Detail.Contains("template", StringComparison.OrdinalIgnoreCase);
        if (!templateRefused || string.IsNullOrWhiteSpace(mail.Html))
            return Report(first);

        _logger.LogWarning("Mail template {Template} was refused by the mail service (is it created, active and allowed for profile {Profile}?). " +
                           "Sending the plain version instead.", mail.TemplateKey, Profile);
        return Report(await PostAsync(RawBody(mail), mail.IdempotencyKey + "-raw", ct));
    }

    private object TemplateBody(OutgoingMail mail) => new
    {
        profile = Profile,
        to = new[] { new { email = mail.ToEmail, name = mail.ToName } },
        template = new { key = mail.TemplateKey, @params = mail.TemplateParams ?? new Dictionary<string, string>() },
        category = "transactional",
        correlation = Correlation(mail),
    };

    private object RawBody(OutgoingMail mail) => new
    {
        profile = Profile,
        to = new[] { new { email = mail.ToEmail, name = mail.ToName } },
        subject = mail.Subject,
        html = mail.Html,
        text = mail.Text,
        category = "transactional",
        correlation = Correlation(mail),
    };

    private static object? Correlation(OutgoingMail mail) =>
        mail.CorrelationType == null ? null : new { entityType = mail.CorrelationType, entityId = mail.CorrelationId };

    private async Task<Result> PostAsync(object body, string idempotencyKey, CancellationToken ct)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, BaseUrl!.TrimEnd('/') + "/messages")
            {
                Content = new StringContent(JsonSerializer.Serialize(body, Json), Encoding.UTF8, "application/json"),
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ApiKey);
            request.Headers.Add("Idempotency-Key", idempotencyKey);

            using var response = await _http.SendAsync(request, ct);
            var status = (int)response.StatusCode;
            if (status is 200 or 202) return new Result(true, status, string.Empty);

            var detail = await response.Content.ReadAsStringAsync(ct);
            return new Result(false, status, detail.Length > 300 ? detail[..300] : detail);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            _logger.LogError(ex, "Mail service could not be reached");
            return new Result(false, 0, "unreachable");
        }
    }

    private bool Report(Result r)
    {
        if (!r.Accepted && r.Status != 0)
            _logger.LogError("Mail service rejected a message: HTTP {Status} {Detail}", r.Status, r.Detail);
        return r.Accepted;
    }
}
