using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using PMS.Application.Common;
using PMS.Application.Common.Interfaces;
using PMS.Application.DTOs.Requests;
using PMS.Application.Services.Implementations;
using PMS.Application.Services.Interfaces;
using PMS.Domain.Entities;
using PMS.Infrastructure.Data;
using PMS.Infrastructure.Services;
using Task = System.Threading.Tasks.Task;

namespace PMS.Tests;

/// <summary>Forgot password: the service rules, and the client for the Techspear Mail Service.</summary>
public class PasswordResetTests
{
    private const string Frontend = "https://projects.example.test";
    private const string OldPassword = "OldPassword1";
    private const string NewPassword = "BrandNewPass9";

    private sealed class FakeMail : IMailSender
    {
        public bool Configured { get; set; } = true;
        public bool Accepts { get; set; } = true;
        public bool IsConfigured => Configured;
        public List<OutgoingMail> Sent { get; } = new();
        public Task<bool> SendAsync(OutgoingMail mail, CancellationToken ct = default)
        {
            Sent.Add(mail);
            return Task.FromResult(Accepts);
        }
    }

    private sealed class World : IDisposable
    {
        public required ApplicationDbContext Db { get; init; }
        public required FakeMail Mail { get; init; }
        public required PasswordResetService Service { get; init; }
        public required User External { get; init; }
        public required User Sso { get; init; }
        public required PasswordHasher Hasher { get; init; }
        public void Dispose() => Db.Dispose();

        public string LastToken()
        {
            var link = Mail.Sent.Last().Text.Split('\n').First(l => l.StartsWith(Frontend));
            return link[(link.IndexOf("token=", StringComparison.Ordinal) + 6)..].Trim();
        }
    }

    private static async Task<World> BuildAsync(Action<FakeMail>? tweak = null)
    {
        var db = TestSupport.NewContext();
        var (_, employee, _, active, _) = await TestSupport.SeedRolesAsync(db);
        var hasher = new PasswordHasher();
        var external = TestSupport.NewUser("ravi@example.com", employee.Id, active.Id, external: true, passwordHash: hasher.Hash(OldPassword));
        var sso = TestSupport.NewUser("sso@example.com", employee.Id, active.Id, external: false);
        db.Users.AddRange(external, sso);
        await db.SaveChangesAsync();
        var mail = new FakeMail();
        tweak?.Invoke(mail);
        return new World
        {
            Db = db, Mail = mail, External = external, Sso = sso, Hasher = hasher,
            Service = new PasswordResetService(NullLogger<PasswordResetService>.Instance, db, hasher, mail, new PasswordResetSettings(Frontend)),
        };
    }

    // ================================================================ requesting a link

    [Fact]
    public async Task A_known_external_user_gets_one_mail_with_a_single_use_link_and_only_the_hash_is_stored()
    {
        using var w = await BuildAsync();

        var outcome = await w.Service.RequestAsync("  Ravi@Example.com ");

        Assert.Equal(ForgotPasswordOutcome.Accepted, outcome);
        var mail = Assert.Single(w.Mail.Sent);
        Assert.Equal("ravi@example.com", mail.ToEmail);
        var raw = w.LastToken();
        Assert.True(raw.Length >= 40);
        Assert.Contains(raw, mail.Html);

        var row = await w.Db.PasswordResetTokens.SingleAsync();
        Assert.DoesNotContain(raw, row.TokenHash);
        Assert.NotEqual(raw, row.TokenHash);
        Assert.Equal(64, row.TokenHash.Length);
        Assert.InRange((row.ExpiresAt - row.CreatedAt).TotalMinutes, 59, 61);
    }

    [Theory]
    [InlineData("nobody@example.com")]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("sso@example.com")]   // Microsoft SSO account: no local password to reset
    public async Task Unknown_empty_and_sso_emails_look_identical_but_send_nothing(string? email)
    {
        using var w = await BuildAsync();

        Assert.Equal(ForgotPasswordOutcome.Accepted, await w.Service.RequestAsync(email));

        Assert.Empty(w.Mail.Sent);
        Assert.Empty(w.Db.PasswordResetTokens);
    }

    [Fact]
    public async Task Inactive_and_deleted_accounts_get_nothing()
    {
        using var w = await BuildAsync();
        w.External.IsActive = false;
        await w.Db.SaveChangesAsync();
        await w.Service.RequestAsync("ravi@example.com");
        w.External.IsActive = true; w.External.IsDeleted = true;
        await w.Db.SaveChangesAsync();
        await w.Service.RequestAsync("ravi@example.com");

        Assert.Empty(w.Mail.Sent);
    }

    [Fact]
    public async Task Without_a_mail_service_the_request_is_reported_unavailable_and_creates_no_token()
    {
        using var w = await BuildAsync(m => m.Configured = false);

        Assert.Equal(ForgotPasswordOutcome.Unavailable, await w.Service.RequestAsync("ravi@example.com"));
        Assert.Empty(w.Db.PasswordResetTokens);
    }

    [Fact]
    public async Task Without_a_frontend_address_the_request_is_unavailable_instead_of_mailing_a_broken_link()
    {
        using var w = await BuildAsync();
        var svc = new PasswordResetService(NullLogger<PasswordResetService>.Instance, w.Db, w.Hasher, w.Mail, new PasswordResetSettings(""));

        Assert.Equal(ForgotPasswordOutcome.Unavailable, await svc.RequestAsync("ravi@example.com"));
        Assert.Empty(w.Mail.Sent);
    }

    [Fact]
    public async Task A_newer_request_cancels_the_older_link()
    {
        using var w = await BuildAsync();
        await w.Service.RequestAsync("ravi@example.com");
        var first = w.LastToken();
        await w.Service.RequestAsync("ravi@example.com");
        var second = w.LastToken();

        Assert.NotEqual(first, second);
        await Assert.ThrowsAsync<InvalidOperationException>(() => w.Service.ResetAsync(first, NewPassword));
        await w.Service.ResetAsync(second, NewPassword);
    }

    [Fact]
    public async Task Requests_are_limited_per_account_per_hour_without_telling_the_caller()
    {
        using var w = await BuildAsync();
        for (var i = 0; i < 6; i++)
            Assert.Equal(ForgotPasswordOutcome.Accepted, await w.Service.RequestAsync("ravi@example.com"));

        Assert.Equal(PMS.Domain.Constants.AppConstants.PasswordResetMaxRequestsPerHour, w.Mail.Sent.Count);
    }

    [Fact]
    public async Task A_mail_service_failure_does_not_change_what_the_caller_sees()
    {
        using var w = await BuildAsync(m => m.Accepts = false);
        Assert.Equal(ForgotPasswordOutcome.Accepted, await w.Service.RequestAsync("ravi@example.com"));
    }

    // ================================================================ using the link

    [Fact]
    public async Task The_link_sets_a_new_password_that_works_for_login_and_the_old_one_stops_working()
    {
        using var w = await BuildAsync();
        await w.Service.RequestAsync("ravi@example.com");

        await w.Service.ResetAsync(w.LastToken(), NewPassword);

        Assert.True(w.Hasher.Verify(NewPassword, (await w.Db.Users.FindAsync(w.External.Id))!.PasswordHash));
        Assert.False(w.Hasher.Verify(OldPassword, (await w.Db.Users.FindAsync(w.External.Id))!.PasswordHash));
        // and a "your password was changed" notice goes to the owner
        Assert.Equal("Your PMS password was changed", w.Mail.Sent.Last().Subject);
    }

    [Fact]
    public async Task A_link_works_only_once()
    {
        using var w = await BuildAsync();
        await w.Service.RequestAsync("ravi@example.com");
        var token = w.LastToken();

        await w.Service.ResetAsync(token, NewPassword);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => w.Service.ResetAsync(token, "AnotherPass77"));
        Assert.Equal(PMS.Domain.Constants.AppConstants.PasswordResetInvalidLinkMessage, ex.Message);
        Assert.True(w.Hasher.Verify(NewPassword, (await w.Db.Users.FindAsync(w.External.Id))!.PasswordHash));
    }

    [Fact]
    public async Task An_expired_link_is_refused()
    {
        using var w = await BuildAsync();
        await w.Service.RequestAsync("ravi@example.com");
        var row = await w.Db.PasswordResetTokens.SingleAsync();
        row.ExpiresAt = DateTime.UtcNow.AddMinutes(-1);
        await w.Db.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() => w.Service.ResetAsync(w.LastToken(), NewPassword));
        Assert.True(w.Hasher.Verify(OldPassword, (await w.Db.Users.FindAsync(w.External.Id))!.PasswordHash));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("not-a-real-token")]
    public async Task Wrong_or_missing_tokens_are_refused_with_the_same_message(string? token)
    {
        using var w = await BuildAsync();
        await w.Service.RequestAsync("ravi@example.com");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => w.Service.ResetAsync(token, NewPassword));
        Assert.Equal(PMS.Domain.Constants.AppConstants.PasswordResetInvalidLinkMessage, ex.Message);
    }

    [Theory]
    [InlineData("short1")]            // too short
    [InlineData("allletterspassword")] // no digit
    [InlineData("12345678901234")]    // no letter
    public async Task A_weak_password_is_refused_and_the_link_stays_usable(string weak)
    {
        using var w = await BuildAsync();
        await w.Service.RequestAsync("ravi@example.com");
        var token = w.LastToken();

        await Assert.ThrowsAsync<InvalidOperationException>(() => w.Service.ResetAsync(token, weak));

        await w.Service.ResetAsync(token, NewPassword); // still valid: a typo should not burn the link
    }

    [Fact]
    public async Task An_account_that_became_sso_or_inactive_cannot_be_reset_with_an_old_link()
    {
        using var w = await BuildAsync();
        await w.Service.RequestAsync("ravi@example.com");
        var token = w.LastToken();
        w.External.IsExternal = false;           // e.g. the owner later signed in with Microsoft
        await w.Db.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() => w.Service.ResetAsync(token, NewPassword));
    }

    // ================================================================ mail service client

    private sealed class CapturingHandler : HttpMessageHandler
    {
        public HttpRequestMessage? Request;                 // the last request
        public string? Body;                                // its body
        public List<(string Key, string Body)> Calls { get; } = new();
        public HttpStatusCode Status = HttpStatusCode.Accepted;
        /// <summary>Answers for call 1, 2, ... (falls back to <see cref="Status"/>). Body text is returned with each answer.</summary>
        public List<(HttpStatusCode Status, string Content)> Script { get; } = new();
        public bool Throw;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            if (Throw) throw new HttpRequestException("boom");
            Request = request;
            Body = request.Content == null ? null : await request.Content.ReadAsStringAsync(ct);
            Calls.Add((request.Headers.GetValues("Idempotency-Key").Single(), Body ?? ""));
            if (Calls.Count <= Script.Count)
            {
                var s = Script[Calls.Count - 1];
                return new HttpResponseMessage(s.Status) { Content = new StringContent(s.Content) };
            }
            return new HttpResponseMessage(Status) { Content = new StringContent("{\"data\":{\"id\":\"msg_1\",\"status\":\"queued\"}}") };
        }
    }

    private static TechspearMailClient Client(CapturingHandler h, Dictionary<string, string?> cfg) =>
        new(new HttpClient(h), new ConfigurationBuilder().AddInMemoryCollection(cfg).Build(), NullLogger<TechspearMailClient>.Instance);

    private static readonly Dictionary<string, string?> Real = new()
    {
        ["MailService:BaseUrl"] = "https://mail.example.test/v1",
        ["MailService:ApiKey"] = "pms-prod.s3cr3t",
    };

    private static OutgoingMail Msg() => new("ravi@example.com", "Ravi", "Subject", "<p>Hi</p>", "Hi", "key-1", "password-reset", "7");

    [Fact]
    public async Task Client_sends_the_request_the_contract_describes()
    {
        var h = new CapturingHandler();
        var client = Client(h, Real);

        Assert.True(client.IsConfigured);
        Assert.True(await client.SendAsync(Msg()));

        Assert.Equal("https://mail.example.test/v1/messages", h.Request!.RequestUri!.ToString());
        Assert.Equal(HttpMethod.Post, h.Request.Method);
        Assert.Equal("Bearer", h.Request.Headers.Authorization!.Scheme);
        Assert.Equal("pms-prod.s3cr3t", h.Request.Headers.Authorization.Parameter);
        Assert.Equal("key-1", h.Request.Headers.GetValues("Idempotency-Key").Single());

        using var doc = JsonDocument.Parse(h.Body!);
        var root = doc.RootElement;
        Assert.Equal("noreply", root.GetProperty("profile").GetString());           // default profile
        Assert.Equal("ravi@example.com", root.GetProperty("to")[0].GetProperty("email").GetString());
        Assert.Equal("Subject", root.GetProperty("subject").GetString());
        Assert.Equal("<p>Hi</p>", root.GetProperty("html").GetString());
        Assert.Equal("transactional", root.GetProperty("category").GetString());
        Assert.Equal("password-reset", root.GetProperty("correlation").GetProperty("entityType").GetString());
        Assert.False(root.TryGetProperty("template", out _)); // raw send, no template
    }

    [Fact]
    public async Task Client_uses_the_configured_profile()
    {
        var h = new CapturingHandler();
        await Client(h, new(Real) { ["MailService:Profile"] = "notify" }).SendAsync(Msg());
        using var doc = JsonDocument.Parse(h.Body!);
        Assert.Equal("notify", doc.RootElement.GetProperty("profile").GetString());
    }

    [Theory]
    [InlineData(HttpStatusCode.OK, true)]
    [InlineData(HttpStatusCode.Accepted, true)]
    [InlineData(HttpStatusCode.BadRequest, false)]
    [InlineData(HttpStatusCode.Unauthorized, false)]
    [InlineData(HttpStatusCode.Forbidden, false)]
    [InlineData(HttpStatusCode.UnprocessableEntity, false)]
    [InlineData(HttpStatusCode.TooManyRequests, false)]
    [InlineData(HttpStatusCode.InternalServerError, false)]
    public async Task Client_reports_success_only_for_200_and_202(HttpStatusCode status, bool expected)
    {
        var h = new CapturingHandler { Status = status };
        Assert.Equal(expected, await Client(h, Real).SendAsync(Msg()));
    }

    [Fact]
    public async Task Client_never_throws_when_the_service_cannot_be_reached()
    {
        var h = new CapturingHandler { Throw = true };
        Assert.False(await Client(h, Real).SendAsync(Msg()));
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("https://mail.example.test/v1", null)]
    [InlineData("https://mail.example.test/v1", "CHANGEME")]
    [InlineData("not a url", "pms-prod.s3cr3t")]
    [InlineData(null, "pms-prod.s3cr3t")]
    public async Task Client_without_a_real_base_url_and_key_is_not_configured_and_sends_nothing(string? url, string? key)
    {
        var h = new CapturingHandler();
        var client = Client(h, new() { ["MailService:BaseUrl"] = url, ["MailService:ApiKey"] = key });

        Assert.False(client.IsConfigured);
        Assert.False(await client.SendAsync(Msg()));
        Assert.Null(h.Request);
    }

    [Fact]
    public async Task Dev_log_only_mode_counts_as_configured_but_sends_nothing_over_http()
    {
        var h = new CapturingHandler();
        var client = Client(h, new() { ["MailService:DevLogOnly"] = "true" });

        Assert.True(client.IsConfigured);
        Assert.True(await client.SendAsync(Msg()));
        Assert.Null(h.Request);
    }

    // ================================================================ templates

    [Fact]
    public async Task The_reset_mail_asks_for_the_pms_reset_template_with_name_link_and_expiry()
    {
        using var w = await BuildAsync();
        w.External.FirstName = "Ravi";
        await w.Db.SaveChangesAsync();

        await w.Service.RequestAsync("ravi@example.com");

        var mail = Assert.Single(w.Mail.Sent);
        Assert.Equal("pms.account.password.reset", mail.TemplateKey);
        Assert.Equal(new[] { "expiresInMinutes", "link", "name" }, mail.TemplateParams!.Keys.OrderBy(k => k).ToArray());
        Assert.Equal("Ravi", mail.TemplateParams["name"]);
        Assert.Equal("60", mail.TemplateParams["expiresInMinutes"]);
        Assert.StartsWith(Frontend + "/reset-password?token=", mail.TemplateParams["link"]);
        Assert.EndsWith(w.LastToken(), mail.TemplateParams["link"]);
        // the plain version stays on the mail as the fallback
        Assert.False(string.IsNullOrWhiteSpace(mail.Html));
        Assert.False(string.IsNullOrWhiteSpace(mail.Text));
    }

    [Fact]
    public async Task A_person_without_a_first_name_is_greeted_as_there()
    {
        using var w = await BuildAsync();
        w.External.FirstName = "";
        await w.Db.SaveChangesAsync();

        await w.Service.RequestAsync("ravi@example.com");

        Assert.Equal("there", w.Mail.Sent.Single().TemplateParams!["name"]);
    }

    [Fact]
    public async Task The_password_changed_notice_asks_for_the_pms_changed_template_with_the_name()
    {
        using var w = await BuildAsync();
        w.External.FirstName = "Ravi";
        await w.Db.SaveChangesAsync();
        await w.Service.RequestAsync("ravi@example.com");

        await w.Service.ResetAsync(w.LastToken(), NewPassword);

        var notice = w.Mail.Sent.Last();
        Assert.Equal("pms.account.password.changed", notice.TemplateKey);
        Assert.Equal(new[] { "name" }, notice.TemplateParams!.Keys.ToArray());
        Assert.Equal("Ravi", notice.TemplateParams["name"]);
    }

    private static OutgoingMail TemplateMsg() => new("ravi@example.com", "Ravi", "Subject", "<p>Hi</p>", "Hi", "key-1", "password-reset", "7",
        TemplateKey: "pms.account.password.reset",
        TemplateParams: new Dictionary<string, string> { ["name"] = "Ravi", ["link"] = "https://x.test/reset?token=abc", ["expiresInMinutes"] = "60" });

    private const string TemplateRefused = "{\"error\":{\"code\":\"VALIDATION_ERROR\",\"details\":[{\"field\":\"template.key\",\"message\":\"unknown\"}]}}";

    [Fact]
    public async Task Client_sends_a_template_request_without_subject_or_html()
    {
        var h = new CapturingHandler();
        Assert.True(await Client(h, Real).SendAsync(TemplateMsg()));

        var call = Assert.Single(h.Calls);
        Assert.Equal("key-1", call.Key);
        using var doc = JsonDocument.Parse(call.Body);
        var root = doc.RootElement;
        Assert.Equal("pms.account.password.reset", root.GetProperty("template").GetProperty("key").GetString());
        var p = root.GetProperty("template").GetProperty("params");
        Assert.Equal("Ravi", p.GetProperty("name").GetString());
        Assert.Equal("https://x.test/reset?token=abc", p.GetProperty("link").GetString());
        Assert.Equal("60", p.GetProperty("expiresInMinutes").GetString());
        // the contract: template OR subject+html, never both
        Assert.False(root.TryGetProperty("subject", out _));
        Assert.False(root.TryGetProperty("html", out _));
        Assert.False(root.TryGetProperty("text", out _));
        Assert.Equal("noreply", root.GetProperty("profile").GetString());
    }

    [Fact]
    public async Task When_the_template_is_refused_the_plain_version_is_sent_once_with_its_own_idempotency_key()
    {
        var h = new CapturingHandler();
        h.Script.Add((HttpStatusCode.BadRequest, TemplateRefused));

        Assert.True(await Client(h, Real).SendAsync(TemplateMsg()));

        Assert.Equal(2, h.Calls.Count);
        Assert.Equal("key-1", h.Calls[0].Key);
        Assert.Equal("key-1-raw", h.Calls[1].Key);
        using var second = JsonDocument.Parse(h.Calls[1].Body);
        Assert.Equal("Subject", second.RootElement.GetProperty("subject").GetString());
        Assert.Equal("<p>Hi</p>", second.RootElement.GetProperty("html").GetString());
        Assert.False(second.RootElement.TryGetProperty("template", out _));
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.UnprocessableEntity)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task Only_a_template_problem_triggers_the_fallback_other_failures_are_not_retried(HttpStatusCode status)
    {
        var h = new CapturingHandler();
        h.Script.Add((status, TemplateRefused));   // even a body that mentions the template: the status decides

        Assert.False(await Client(h, Real).SendAsync(TemplateMsg()));
        Assert.Single(h.Calls);
    }

    [Fact]
    public async Task A_400_that_is_not_about_the_template_is_not_retried()
    {
        var h = new CapturingHandler();
        h.Script.Add((HttpStatusCode.BadRequest, "{\"error\":{\"details\":[{\"field\":\"to[0].email\",\"message\":\"invalid\"}]}}"));

        Assert.False(await Client(h, Real).SendAsync(TemplateMsg()));
        Assert.Single(h.Calls);
    }

    [Fact]
    public async Task If_even_the_plain_version_fails_the_send_is_reported_as_failed()
    {
        var h = new CapturingHandler();
        h.Script.Add((HttpStatusCode.BadRequest, TemplateRefused));
        h.Script.Add((HttpStatusCode.Unauthorized, "{}"));

        Assert.False(await Client(h, Real).SendAsync(TemplateMsg()));
        Assert.Equal(2, h.Calls.Count);
    }

    [Fact]
    public async Task A_template_mail_with_no_plain_version_is_not_retried_when_the_template_is_refused()
    {
        var h = new CapturingHandler();
        h.Script.Add((HttpStatusCode.BadRequest, TemplateRefused));
        var mail = TemplateMsg() with { Html = "", Text = "" };

        Assert.False(await Client(h, Real).SendAsync(mail));
        Assert.Single(h.Calls);
    }

    [Fact]
    public async Task Dev_log_only_mode_still_sends_nothing_for_template_mail()
    {
        var h = new CapturingHandler();
        Assert.True(await Client(h, new() { ["MailService:DevLogOnly"] = "true" }).SendAsync(TemplateMsg()));
        Assert.Empty(h.Calls);
    }
}
