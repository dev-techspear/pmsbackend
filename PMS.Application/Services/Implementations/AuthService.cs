using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PMS.Application.Common;
using PMS.Application.Common.Interfaces;
using PMS.Application.DTOs.Requests;
using PMS.Application.DTOs.Responses;
using PMS.Application.Services.Interfaces;
using PMS.Domain.Constants;
using PMS.Domain.Entities;
using System.Net.Mail;
using System.Security.Claims;
using System.Text.Json.Nodes;
using Task = System.Threading.Tasks.Task;

namespace PMS.Application.Services.Implementations;

public class AuthService : BaseService<AuthService>, IAuthService
{
    private readonly IApplicationDbContext _context;
    private readonly ITokenService _tokenService;
    private readonly IConfiguration _configuration;
    private readonly IPasswordHasher _passwordHasher;
    private static readonly HttpClient _httpClient = new HttpClient();

    public AuthService(
        ILogger<AuthService> logger, 
        IApplicationDbContext context, 
        ITokenService tokenService, 
        IConfiguration configuration,
        IPasswordHasher passwordHasher) : base(logger)
    {
        _passwordHasher = passwordHasher;
        _context = context;
        _tokenService = tokenService;
        _configuration = configuration;
    }

    public async Task<TokenResponse> HandleMicrosoftCallbackAsync(MSCallbackRequest payload)
    {
        return await ExecuteAsync(async () =>
        {
            var tenantId = _configuration["AzureAd:TenantId"];
            var clientId = _configuration["AzureAd:ClientId"];
            var clientSecret = _configuration["AzureAd:ClientSecret"];

            if (string.IsNullOrEmpty(tenantId) || string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            {
                throw new InvalidOperationException("Microsoft SSO configurations are incomplete on this server.");
            }

            // 1. Exchange auth code for Microsoft tokens
            var tokenUrl = $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token";
            var tokenRequestParams = new Dictionary<string, string>
            {
                { "grant_type", "authorization_code" },
                { "code", payload.Code },
                { "redirect_uri", payload.RedirectUri },
                { "client_id", clientId },
                { "client_secret", clientSecret },
                { "scope", "openid profile email User.Read" }
            };

            HttpResponseMessage tokenResponse;
            try
            {
                tokenResponse = await _httpClient.PostAsync(tokenUrl, new FormUrlEncodedContent(tokenRequestParams));
                tokenResponse.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Microsoft rejected the authorization code or is unreachable: {ex.Message}");
            }

            var tokenResponseStr = await tokenResponse.Content.ReadAsStringAsync();
            var tokenJson = JsonNode.Parse(tokenResponseStr);
            var msAccessToken = tokenJson?["access_token"]?.ToString();

            if (string.IsNullOrEmpty(msAccessToken))
            {
                throw new InvalidOperationException("Failed to retrieve access token from Microsoft response.");
            }

            // 2. Query Microsoft Graph Profile
            var graphUrl = "https://graph.microsoft.com/v1.0/me?$select=id,mail,otherMails,userPrincipalName,givenName,surname,displayName,userType";
            var graphRequest = new HttpRequestMessage(HttpMethod.Get, graphUrl);
            graphRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", msAccessToken);

            HttpResponseMessage graphResponse;
            try
            {
                graphResponse = await _httpClient.SendAsync(graphRequest);
                graphResponse.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to fetch profile from Microsoft Graph: {ex.Message}");
            }

            var graphResponseStr = await graphResponse.Content.ReadAsStringAsync();
            var graphJson = JsonNode.Parse(graphResponseStr);

            var o365Id = graphJson?["id"]?.ToString();
            var isGuest = string.Equals(graphJson?["userType"]?.ToString(), AppConstants.GraphUserTypeGuest, StringComparison.OrdinalIgnoreCase);
            // B2B guests usually have no `mail`, and their UPN is name_domain#EXT#@tenant - use otherMails instead.
            var email = graphJson?["mail"]?.ToString()
                        ?? (isGuest ? graphJson?["otherMails"]?.AsArray().FirstOrDefault()?.ToString() : null)
                        ?? graphJson?["userPrincipalName"]?.ToString();
            var firstName = graphJson?["givenName"]?.ToString() ?? "";
            var lastName = graphJson?["surname"]?.ToString() ?? "";
            var displayName = graphJson?["displayName"]?.ToString();

            if (string.IsNullOrEmpty(o365Id))
            {
                throw new InvalidOperationException("O365 ID is missing in Microsoft profile response.");
            }

            // 3. Upsert user profile in database
            var user = await SyncUserAsync(o365Id, email, firstName, lastName, displayName, isGuest);

            if (user.IsDeleted || !user.IsActive)
            {
                throw new UnauthorizedAccessException("Account is inactive or blocked.");
            }

            // 4. Generate local token response
            return BuildTokenResponse(user);
        }, nameof(HandleMicrosoftCallbackAsync));
    }

    public async Task<TokenResponse> LoginExternalAsync(LoginRequest payload)
    {
        return await ExecuteAsync(async () =>
        {
            var email = (payload.Email ?? "").Trim().ToLowerInvariant();

            var user = string.IsNullOrEmpty(email)
                ? null
                : await _context.Users
                    .Include(u => u.Role)
                    .Include(u => u.Status)
                    .FirstOrDefaultAsync(u => u.Email == email && u.IsExternal);

            // Verify even when the user is missing so timing does not reveal which emails exist.
            var passwordOk = _passwordHasher.Verify(payload.Password ?? "", user?.PasswordHash);
            if (user == null || !passwordOk)
            {
                throw new UnauthorizedAccessException(AppConstants.InvalidCredentialsMessage);
            }

            if (user.IsDeleted || !user.IsActive)
            {
                throw new UnauthorizedAccessException("Account is inactive or blocked.");
            }

            if (user.Status?.Name != AppConstants.UserStatusActive)
            {
                throw new UnauthorizedAccessException(AppConstants.AccountPendingMessage);
            }

            return BuildTokenResponse(user);
        }, nameof(LoginExternalAsync));
    }

    public async Task<RegisterResponse> RegisterExternalAsync(RegisterRequest payload)
    {
        return await ExecuteAsync(async () =>
        {
            var email = (payload.Email ?? "").Trim().ToLowerInvariant();
            var firstName = (payload.FirstName ?? "").Trim();
            var lastName = (payload.LastName ?? "").Trim();

            var errors = new List<string>();
            if (firstName.Length is 0 or > 100) errors.Add("First name is required (max 100 characters).");
            if (lastName.Length > 100) errors.Add("Last name must be 100 characters or fewer.");
            if (!IsValidEmail(email)) errors.Add("A valid email address is required.");
            errors.AddRange(ValidatePassword(payload.Password));
            if (errors.Count > 0)
            {
                throw new InvalidOperationException(string.Join(" ", errors));
            }

            // Same response whether or not the email is taken, so this endpoint cannot be used
            // to discover which addresses have accounts.
            var response = new RegisterResponse(AppConstants.RegistrationReceivedMessage);
            if (await _context.Users.AnyAsync(u => u.Email == email))
            {
                LogWarn("Registration ignored: email already has an account.");
                return response;
            }

            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == AppConstants.RoleFreelancer);
            var status = await _context.UserStatuses.FirstOrDefaultAsync(s => s.Name == AppConstants.UserStatusPending);

            var baseUsername = email.Split('@')[0].Replace(".", "_");
            var username = await _context.Users.AnyAsync(u => u.Username == baseUsername)
                ? $"{baseUsername}_{Guid.NewGuid().ToString("N").Substring(0, 8)}"
                : baseUsername;

            _context.Users.Add(new User
            {
                PublicId = "USR-" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper(),
                EmployeeId = "EXT-" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper(),
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                Username = username,
                DisplayName = $"{firstName} {lastName}".Trim(),
                PasswordHash = _passwordHasher.Hash(payload.Password),
                IsExternal = true,
                IsSynced = false,
                RoleId = role?.Id,
                StatusId = status?.Id
            });

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                // Lost a race with a concurrent registration of the same email.
                LogWarn("Registration save failed, treating as duplicate: {Message}", ex.GetBaseException().Message);
            }

            return response;
        }, nameof(RegisterExternalAsync));
    }

    private static bool IsValidEmail(string email)
    {
        if (email.Length is 0 or > 254) return false;
        return MailAddress.TryCreate(email, out var parsed) && parsed.Address == email && email.Contains('.', StringComparison.Ordinal);
    }

    private static IEnumerable<string> ValidatePassword(string? password)
    {
        if (string.IsNullOrEmpty(password) || password.Length < AppConstants.PasswordMinLength)
        {
            yield return $"Password must be at least {AppConstants.PasswordMinLength} characters.";
            yield break;
        }
        if (password.Length > AppConstants.PasswordMaxLength)
            yield return $"Password must be at most {AppConstants.PasswordMaxLength} characters.";
        if (!password.Any(char.IsLetter) || !password.Any(char.IsDigit))
            yield return "Password must contain at least one letter and one number.";
    }

    public async Task<TokenResponse> RefreshTokenAsync(RefreshTokenRequest payload)
    {
        return await ExecuteAsync(async () =>
        {
            ClaimsPrincipal? principal;
            try
            {
                principal = _tokenService.GetPrincipalFromExpiredToken(payload.RefreshToken);
            }
            catch (Exception ex)
            {
                throw new UnauthorizedAccessException($"Invalid or expired refresh token: {ex.Message}");
            }

            var typeClaim = principal?.Claims.FirstOrDefault(c => c.Type == "type")?.Value;
            if (typeClaim != "refresh")
            {
                throw new UnauthorizedAccessException("Invalid token type.");
            }

            var userIdStr = principal?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                throw new UnauthorizedAccessException("Invalid token payload claims.");
            }

            var user = await _context.Users
                .Include(u => u.Role)
                .Include(u => u.Status)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null || user.IsDeleted || !user.IsActive)
            {
                throw new UnauthorizedAccessException("User not found or inactive.");
            }

            return BuildTokenResponse(user);
        }, nameof(RefreshTokenAsync));
    }

    private async Task<User> SyncUserAsync(string o365Id, string? email, string firstName, string lastName, string? displayName, bool isGuest)
    {
        var user = await _context.Users
            .Include(u => u.Role)
            .Include(u => u.Status)
            .FirstOrDefaultAsync(u => u.O365Id == o365Id);

        if (user == null && !string.IsNullOrEmpty(email))
        {
            user = await _context.Users
                .Include(u => u.Role)
                .Include(u => u.Status)
                .FirstOrDefaultAsync(u => u.Email == email.ToLower());
        }

        if (user != null)
        {
            // Entra has now vouched for this email. If someone pre-registered it with a password,
            // that password must not survive: it could belong to an account squatter.
            if (user.PasswordHash != null)
            {
                LogWarn("SSO login matched an account with a local password (user {UserId}); clearing the password.", user.Id);
                user.PasswordHash = null;
            }

            user.O365Id = o365Id;
            user.IsSynced = true;
            user.FirstName = firstName;
            user.LastName = lastName;
            user.DisplayName = displayName ?? $"{firstName} {lastName}".Trim();

            if (user.RoleId == null)
            {
                var defaultRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == (isGuest ? AppConstants.RoleFreelancer : AppConstants.RoleEmployee));
                if (defaultRole != null) user.RoleId = defaultRole.Id;
            }

            if (user.StatusId == null)
            {
                var defaultStatus = await _context.UserStatuses.FirstOrDefaultAsync(s => s.Name == "Active");
                if (defaultStatus != null) user.StatusId = defaultStatus.Id;
            }

            await _context.SaveChangesAsync();
            return user;
        }

        if (string.IsNullOrEmpty(email))
        {
            throw new InvalidOperationException("Email is required to create a new SSO user record.");
        }

        var defaultRoleForNewUser = await _context.Roles.FirstOrDefaultAsync(r => r.Name == (isGuest ? AppConstants.RoleFreelancer : AppConstants.RoleEmployee));
        var defaultStatusForNewUser = await _context.UserStatuses.FirstOrDefaultAsync(s => s.Name == "Active");

        var baseUsername = email.Split('@')[0].ToLower().Replace(".", "_");
        var username = baseUsername;

        var usernameExists = await _context.Users.AnyAsync(u => u.Username == username);
        if (usernameExists)
        {
            username = $"{baseUsername}_{Guid.NewGuid().ToString("N").Substring(0, 8)}";
        }

        var newUser = new User
        {
            PublicId = "USR-" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper(),
            EmployeeId = "EMP-" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper(),
            FirstName = string.IsNullOrEmpty(firstName) ? email.Split('@')[0] : firstName,
            LastName = lastName ?? "",
            Email = email.ToLower(),
            Username = username,
            DisplayName = displayName ?? $"{firstName} {lastName}".Trim(),
            O365Id = o365Id,
            IsSynced = true,
            IsExternal = isGuest,
            RoleId = defaultRoleForNewUser?.Id,
            StatusId = defaultStatusForNewUser?.Id
        };

        _context.Users.Add(newUser);
        await _context.SaveChangesAsync();

        return await _context.Users
            .Include(u => u.Role)
            .Include(u => u.Status)
            .FirstAsync(u => u.Id == newUser.Id);
    }

    private TokenResponse BuildTokenResponse(User user)
    {
        var accessToken = _tokenService.GenerateAccessToken(user);
        var refreshToken = _tokenService.GenerateRefreshToken(user);

        RoleResponseDto? roleData = null;
        if (user.Role != null)
        {
            JsonNode? permsNode = null;
            try
            {
                permsNode = JsonNode.Parse(user.Role.Permissions ?? "{}");
            }
            catch { permsNode = JsonNode.Parse("{}"); }

            roleData = new RoleResponseDto(user.Role.Id, user.Role.Name, permsNode);
        }

        return new TokenResponse(
            accessToken,
            refreshToken,
            "bearer",
            user.Id,
            user.PublicId,
            user.Email,
            user.FirstName ?? "",
            user.LastName ?? "",
            user.DisplayName,
            roleData,
            user.IsExternal,
            user.IsSynced
        );
    }

    public async Task<UserProfileResponse?> GetUserProfileAsync(int userId)
    {
        return await ExecuteAsync(async () =>
        {
            var user = await _context.Users
                .Include(u => u.Role)
                .Include(u => u.Status)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null) return null;

            RoleResponseDto? roleData = null;
            if (user.Role != null)
            {
                JsonNode? permsNode = null;
                try
                {
                    permsNode = JsonNode.Parse(user.Role.Permissions ?? "{}");
                }
                catch { permsNode = JsonNode.Parse("{}"); }

                roleData = new RoleResponseDto(user.Role.Id, user.Role.Name, permsNode);
            }

            return new UserProfileResponse(
                user.Id,
                user.PublicId,
                user.Email,
                user.FirstName ?? "",
                user.LastName ?? "",
                user.DisplayName,
                roleData,
                user.IsExternal,
                user.IsSynced
            );
        }, nameof(GetUserProfileAsync));
    }
}
