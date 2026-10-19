using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using PMS.Application.Common.Interfaces;
using PMS.Application.DTOs.Requests;
using PMS.Application.Services.Implementations;
using PMS.Domain.Constants;
using PMS.Domain.Entities;
using PMS.Infrastructure.Data;
using PMS.Infrastructure.Services;

using Task = System.Threading.Tasks.Task;
namespace PMS.Tests;

public class ExternalAuthTests
{
    private sealed class FakeTokenService : ITokenService
    {
        public string GenerateAccessToken(User user) => "access-" + user.Id;
        public string GenerateRefreshToken(User user) => "refresh-" + user.Id;
        public string GenerateChatTicket(int userId) => "chat-ticket-" + userId;
        public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token) => null;
    }

    private static AuthService NewService(ApplicationDbContext db) => new(
        NullLogger<AuthService>.Instance, db, new FakeTokenService(),
        new ConfigurationBuilder().Build(), new PasswordHasher());

    private const string GoodPassword = "Sturdy-pass-2026";

    private static RegisterRequest Reg(string email = "Free.Lancer@Example.com", string password = GoodPassword) =>
        new("Free", "Lancer", email, password);

    [Fact]
    public async Task Register_creates_a_pending_external_freelancer_with_a_hashed_password()
    {
        using var db = TestSupport.NewContext();
        await TestSupport.SeedRolesAsync(db);

        var response = await NewService(db).RegisterExternalAsync(Reg());

        Assert.Equal(AppConstants.RegistrationReceivedMessage, response.Message);
        var user = await db.Users.Include(u => u.Role).Include(u => u.Status).SingleAsync();
        Assert.True(user.IsExternal);
        Assert.Equal("free.lancer@example.com", user.Email);
        Assert.Equal(AppConstants.RoleFreelancer, user.Role!.Name);
        Assert.Equal(AppConstants.UserStatusPending, user.Status!.Name);
        Assert.NotNull(user.PasswordHash);
        Assert.DoesNotContain(GoodPassword, user.PasswordHash);
    }

    [Fact]
    public async Task Register_with_an_existing_email_looks_identical_and_changes_nothing()
    {
        using var db = TestSupport.NewContext();
        var (_, employee, _, active, _) = await TestSupport.SeedRolesAsync(db);
        db.Users.Add(TestSupport.NewUser("taken@example.com", employee.Id, active.Id));
        await db.SaveChangesAsync();

        var response = await NewService(db).RegisterExternalAsync(Reg("taken@example.com"));

        Assert.Equal(AppConstants.RegistrationReceivedMessage, response.Message);
        var only = await db.Users.SingleAsync();
        Assert.False(only.IsExternal);
        Assert.Null(only.PasswordHash);
    }

    [Theory]
    [InlineData("not-an-email", GoodPassword)]
    [InlineData("a@b.com", "short1")]
    [InlineData("a@b.com", "onlyletterslongenough")]
    [InlineData("a@b.com", "1234567890123")]
    public async Task Register_rejects_bad_email_or_weak_password(string email, string password)
    {
        using var db = TestSupport.NewContext();
        await TestSupport.SeedRolesAsync(db);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => NewService(db).RegisterExternalAsync(Reg(email, password)));
        Assert.Empty(db.Users);
    }

    [Fact]
    public async Task Login_is_refused_while_pending_and_works_once_the_status_is_active()
    {
        using var db = TestSupport.NewContext();
        var (_, _, _, active, _) = await TestSupport.SeedRolesAsync(db);
        var service = NewService(db);
        await service.RegisterExternalAsync(Reg("pending@example.com"));

        var pending = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.LoginExternalAsync(new LoginRequest("pending@example.com", GoodPassword)));
        Assert.Equal(AppConstants.AccountPendingMessage, pending.Message);

        var user = await db.Users.SingleAsync();
        user.StatusId = active.Id;
        await db.SaveChangesAsync();

        var token = await service.LoginExternalAsync(new LoginRequest("Pending@Example.com", GoodPassword));
        Assert.StartsWith("access-", token.AccessToken);
        Assert.True(token.IsExternal);
        Assert.Equal(AppConstants.RoleFreelancer, token.Role!.Name);
    }

    [Fact]
    public async Task Wrong_password_and_unknown_email_give_the_same_error()
    {
        using var db = TestSupport.NewContext();
        var (_, _, _, active, _) = await TestSupport.SeedRolesAsync(db);
        var service = NewService(db);
        await service.RegisterExternalAsync(Reg("real@example.com"));
        (await db.Users.SingleAsync()).StatusId = active.Id;
        await db.SaveChangesAsync();

        var wrong = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.LoginExternalAsync(new LoginRequest("real@example.com", "Wrong-pass-2026")));
        var unknown = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.LoginExternalAsync(new LoginRequest("ghost@example.com", GoodPassword)));

        Assert.Equal(AppConstants.InvalidCredentialsMessage, wrong.Message);
        Assert.Equal(wrong.Message, unknown.Message);
    }

    [Fact]
    public async Task Password_login_is_closed_to_internal_users_even_with_a_stored_hash()
    {
        using var db = TestSupport.NewContext();
        var (_, employee, _, active, _) = await TestSupport.SeedRolesAsync(db);
        db.Users.Add(TestSupport.NewUser("staff@example.com", employee.Id, active.Id,
            external: false, passwordHash: new PasswordHasher().Hash(GoodPassword)));
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => NewService(db).LoginExternalAsync(new LoginRequest("staff@example.com", GoodPassword)));
    }

    [Fact]
    public async Task A_disabled_external_account_cannot_log_in()
    {
        using var db = TestSupport.NewContext();
        var (_, _, freelancer, active, _) = await TestSupport.SeedRolesAsync(db);
        var user = TestSupport.NewUser("off@example.com", freelancer.Id, active.Id,
            external: true, passwordHash: new PasswordHasher().Hash(GoodPassword));
        user.IsActive = false;
        db.Users.Add(user);
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => NewService(db).LoginExternalAsync(new LoginRequest("off@example.com", GoodPassword)));
    }
}
