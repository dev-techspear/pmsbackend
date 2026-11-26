using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Abstractions;
using PMS.Application.Common;
using PMS.Application.Common.Interfaces;
using PMS.Application.DTOs.Requests;
using PMS.Application.Services.Implementations;
using PMS.Domain.Entities;
using PMS.Infrastructure.Data;
using PMS.Infrastructure.Services;
using Task = System.Threading.Tasks.Task;

namespace PMS.Tests;

/// <summary>A password reset signs the account out everywhere: access tokens and refresh tokens issued before it stop working.</summary>
public class SessionRevocationTests
{
    // ================================================================ the rule

    private static string At(DateTime utc) => new DateTimeOffset(utc, TimeSpan.Zero).ToUnixTimeSeconds().ToString();
    private static readonly DateTime Cutoff = new(2026, 11, 26, 12, 0, 30, 400, DateTimeKind.Utc);

    [Fact]
    public void Nobody_is_revoked_when_the_account_was_never_reset()
    {
        Assert.False(SessionRevocation.IsRevoked(null, At(Cutoff.AddDays(-30))));
        Assert.False(SessionRevocation.IsRevoked(null, null));
    }

    [Theory]
    [InlineData(-3600, true)]   // an hour before the reset
    [InlineData(-1, true)]      // just before
    [InlineData(0, true)]       // the same second: treated as old
    [InlineData(1, false)]      // the next second: a fresh sign-in
    [InlineData(3600, false)]
    public void Tokens_are_revoked_only_when_issued_at_or_before_the_reset_second(int offsetSeconds, bool revoked)
    {
        Assert.Equal(revoked, SessionRevocation.IsRevoked(Cutoff, At(Cutoff.AddSeconds(offsetSeconds))));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-number")]
    public void A_token_with_no_readable_issue_time_predates_the_feature_and_is_revoked(string? iat)
    {
        Assert.True(SessionRevocation.IsRevoked(Cutoff, iat));
    }

    // ================================================================ tokens carry their issue time

    private static TokenService Tokens() => new(new ConfigurationBuilder().AddInMemoryCollection(
        new Dictionary<string, string?> { ["SECRET_KEY"] = new string('k', 64) }).Build());

    [Fact]
    public void Access_and_refresh_tokens_are_stamped_with_the_time_they_were_issued()
    {
        var user = TestSupport.NewUser("a@x.com", null, null);
        user.Id = 7;
        var before = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        foreach (var token in new[] { Tokens().GenerateAccessToken(user), Tokens().GenerateRefreshToken(user) })
        {
            var iat = new JwtSecurityTokenHandler().ReadJwtToken(token).Claims.Single(c => c.Type == "iat").Value;
            Assert.InRange(long.Parse(iat), before, before + 5);
        }
    }

    // ================================================================ reset marks the account, refresh honours it

    private const string OldPassword = "OldPassword1";
    private const string NewPassword = "BrandNewPass9";

    private sealed class NoMail : IMailSender
    {
        public bool IsConfigured => true;
        public Task<bool> SendAsync(OutgoingMail mail, CancellationToken ct = default) => Task.FromResult(true);
    }

    private static string Sha(string raw) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw))).ToLowerInvariant();

    [Fact]
    public async Task Resetting_the_password_stamps_the_account_and_old_refresh_tokens_stop_working_new_ones_work()
    {
        using var db = TestSupport.NewContext();
        var (_, employee, _, active, _) = await TestSupport.SeedRolesAsync(db);
        var hasher = new PasswordHasher();
        var user = TestSupport.NewUser("ravi@example.com", employee.Id, active.Id, external: true, passwordHash: hasher.Hash(OldPassword));
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var tokens = Tokens();
        var auth = new AuthService(NullLogger<AuthService>.Instance, db, tokens, new ConfigurationBuilder().Build(), hasher);
        var reset = new PasswordResetService(NullLogger<PasswordResetService>.Instance, db, hasher, new NoMail(), new PasswordResetSettings("https://x.test"));

        var oldTokens = await auth.LoginExternalAsync(new LoginRequest("ravi@example.com", OldPassword));
        // before the reset the refresh token works
        Assert.NotNull(await auth.RefreshTokenAsync(new RefreshTokenRequest(oldTokens.RefreshToken)));

        db.PasswordResetTokens.Add(new PasswordResetToken { UserId = user.Id, TokenHash = Sha("raw-token"), ExpiresAt = DateTime.UtcNow.AddMinutes(30) });
        await db.SaveChangesAsync();
        await reset.ResetAsync("raw-token", NewPassword);

        Assert.NotNull((await db.Users.FindAsync(user.Id))!.SessionsValidFrom);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => auth.RefreshTokenAsync(new RefreshTokenRequest(oldTokens.RefreshToken)));

        await Task.Delay(1100); // tokens issued in the same second as the reset count as old
        var fresh = await auth.LoginExternalAsync(new LoginRequest("ravi@example.com", NewPassword));
        Assert.NotNull(await auth.RefreshTokenAsync(new RefreshTokenRequest(fresh.RefreshToken)));
    }

    // ================================================================ through the real HTTP pipeline

    [Fact]
    public async Task A_reset_signs_the_user_out_of_the_running_app_but_other_users_and_new_sign_ins_are_fine()
    {
        var dbName = Guid.NewGuid().ToString();
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, cfg) => cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Server=unused;Database=unused;",
            }));
            builder.ConfigureServices(services =>
            {
                services.RemoveAll(typeof(IDbContextOptionsConfiguration<ApplicationDbContext>));
                services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
                services.AddDbContext<ApplicationDbContext>(o => o.UseInMemoryDatabase(dbName));
            });
        });

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
            var role = await db.Roles.FirstAsync(r => r.Name == "Employee");
            var active = await db.UserStatuses.FirstAsync(s => s.Name == "Active");
            var a = TestSupport.NewUser("ravi@e2e.com", role.Id, active.Id, external: true, passwordHash: hasher.Hash(OldPassword));
            var b = TestSupport.NewUser("other@e2e.com", role.Id, active.Id, external: true, passwordHash: hasher.Hash(OldPassword));
            db.Users.AddRange(a, b);
            await db.SaveChangesAsync();
            db.PasswordResetTokens.Add(new PasswordResetToken { UserId = a.Id, TokenHash = Sha("e2e-raw"), ExpiresAt = DateTime.UtcNow.AddMinutes(30) });
            await db.SaveChangesAsync();
        }

        var client = factory.CreateClient();
        async Task<(string access, string refresh)> Login(string email, string password)
        {
            var res = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password });
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);
            var data = JsonDocument.Parse(await res.Content.ReadAsStringAsync()).RootElement.GetProperty("data");
            return (data.GetProperty("access_token").GetString()!, data.GetProperty("refresh_token").GetString()!);
        }
        async Task<HttpStatusCode> Me(string access)
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", access);
            return (await client.SendAsync(req)).StatusCode;
        }

        var raviOld = await Login("ravi@e2e.com", OldPassword);
        var otherOld = await Login("other@e2e.com", OldPassword);
        Assert.Equal(HttpStatusCode.OK, await Me(raviOld.access));

        var reset = await client.PostAsJsonAsync("/api/v1/auth/reset-password", new { token = "e2e-raw", new_password = NewPassword });
        Assert.Equal(HttpStatusCode.OK, reset.StatusCode);

        // the reset user is signed out: the old access token is refused, and the old refresh token cannot get a new one
        Assert.Equal(HttpStatusCode.Unauthorized, await Me(raviOld.access));
        var refresh = await client.PostAsJsonAsync("/api/v1/auth/refresh", new { refresh_token = raviOld.refresh });
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);

        // someone else's session is untouched
        Assert.Equal(HttpStatusCode.OK, await Me(otherOld.access));

        // and signing in again with the new password works
        await Task.Delay(1100);
        var raviNew = await Login("ravi@e2e.com", NewPassword);
        Assert.Equal(HttpStatusCode.OK, await Me(raviNew.access));
    }
}
