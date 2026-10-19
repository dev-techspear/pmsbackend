using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PMS.Application.Common;
using PMS.Application.Common.Interfaces;
using PMS.Application.DTOs.Requests;
using PMS.Application.DTOs.Responses;
using PMS.Application.Services.Interfaces;
using PMS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using Task = System.Threading.Tasks.Task;

namespace PMS.Application.Services.Implementations;

public class UserService : BaseService<UserService>, IUserService
{
    private readonly IApplicationDbContext _context;
    private readonly IChatAccessSync _chatAccess;
    private readonly IConfiguration _configuration;
    private static readonly HttpClient _httpClient = new HttpClient();

    public UserService(ILogger<UserService> logger, IApplicationDbContext context, IConfiguration configuration, IChatAccessSync chatAccess) : base(logger)
    {
        _context = context;
        _chatAccess = chatAccess;
        _configuration = configuration;
    }

    public async Task<UserListResponseDto> GetUsersAsync(int skip, int limit, string? search, IEnumerable<int>? roleIds, bool? isActive)
    {
        return await ExecuteAsync(async () =>
        {
            var queryable = _context.Users
                .Include(u => u.Role)
                .Include(u => u.Status)
                .Include(u => u.Manager)
                .Include(u => u.Skills)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                var searchLower = search.ToLower();
                queryable = queryable.Where(u => u.FirstName.ToLower().Contains(searchLower) ||
                                                 (u.LastName != null && u.LastName.ToLower().Contains(searchLower)) ||
                                                 u.Email.ToLower().Contains(searchLower) ||
                                                 u.Username.ToLower().Contains(searchLower) ||
                                                 (u.DisplayName != null && u.DisplayName.ToLower().Contains(searchLower)));
            }

            if (roleIds != null && roleIds.Any())
            {
                queryable = queryable.Where(u => u.RoleId != null && roleIds.Contains(u.RoleId.Value));
            }

            if (isActive.HasValue)
            {
                queryable = queryable.Where(u => u.IsActive == isActive.Value);
            }

            var total = await queryable.CountAsync();

            var items = await queryable
                .OrderBy(u => u.Id)
                .Skip(skip)
                .Take(limit)
                .ToListAsync();

            var dtos = items.Select(MapToUserDto);
            return new UserListResponseDto(total, dtos);
        }, nameof(GetUsersAsync));
    }

    public async Task<UserResponseDto?> GetUserByIdAsync(int id)
    {
        return await ExecuteAsync(async () =>
        {
            var user = await _context.Users
                .Include(u => u.Role)
                .Include(u => u.Status)
                .Include(u => u.Manager)
                .Include(u => u.Skills)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null) return null;
            return MapToUserDto(user);
        }, nameof(GetUserByIdAsync));
    }

    public async Task<UserResponseDto?> GetUserByEmailAsync(string email)
    {
        return await ExecuteAsync(async () =>
        {
            var user = await _context.Users
                .Include(u => u.Role)
                .Include(u => u.Status)
                .Include(u => u.Manager)
                .Include(u => u.Skills)
                .FirstOrDefaultAsync(u => u.Email == email.ToLower());

            if (user == null) return null;
            return MapToUserDto(user);
        }, nameof(GetUserByEmailAsync));
    }

    public async Task<UserResponseDto?> GetUserByUsernameAsync(string username)
    {
        return await ExecuteAsync(async () =>
        {
            var user = await _context.Users
                .Include(u => u.Role)
                .Include(u => u.Status)
                .Include(u => u.Manager)
                .Include(u => u.Skills)
                .FirstOrDefaultAsync(u => u.Username == username);

            if (user == null) return null;
            return MapToUserDto(user);
        }, nameof(GetUserByUsernameAsync));
    }

    public async Task<UserResponseDto> CreateUserAsync(UserCreateRequest request, string actorId)
    {
        return await ExecuteAsync(async () =>
        {
            var baseUsername = request.Email.Split('@')[0].ToLower().Replace(".", "_");
            var username = request.Username ?? baseUsername;

            var usernameExists = await _context.Users.AnyAsync(u => u.Username == username);
            if (usernameExists)
            {
                username = $"{baseUsername}_{Guid.NewGuid().ToString("N").Substring(0, 8)}";
            }

            var user = new User
            {
                PublicId = "USR-" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper(),
                EmployeeId = request.EmployeeId ?? "EMP-" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper(),
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email.ToLower(),
                Username = username,
                Phone = request.Phone,
                JobTitle = request.JobTitle,
                JoinDate = request.JoinDate,
                RoleId = request.RoleId,
                StatusId = request.StatusId,
                O365Id = request.O365Id,
                ManagerEmail = request.ManagerEmail,
                DisplayName = request.DisplayName ?? $"{request.FirstName} {request.LastName}".Trim(),
                Gender = request.Gender,
                Country = request.Country,
                State = request.State,
                Language = request.Language,
                Timezone = request.Timezone,
                IsExternal = request.IsExternal,
                IsSynced = request.IsSynced
            };

            if (request.SkillIds != null && request.SkillIds.Any())
            {
                var skills = await _context.Skills
                    .Where(s => request.SkillIds.Contains(s.Id))
                    .ToListAsync();
                foreach (var skill in skills)
                {
                    user.Skills.Add(skill);
                }
            }

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var reloadedUser = await _context.Users
                .Include(u => u.Role)
                .Include(u => u.Status)
                .Include(u => u.Manager)
                .Include(u => u.Skills)
                .FirstAsync(u => u.Id == user.Id);

            return MapToUserDto(reloadedUser);
        }, nameof(CreateUserAsync));
    }

    public async Task<UserResponseDto?> UpdateUserAsync(int id, UserUpdateRequest request, string actorId)
    {
        return await ExecuteAsync(async () =>
        {
            var user = await _context.Users
                .Include(u => u.Role)
                .Include(u => u.Status)
                .Include(u => u.Manager)
                .Include(u => u.Skills)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null) return null;

            if (request.FirstName != null) user.FirstName = request.FirstName;
            if (request.LastName != null) user.LastName = request.LastName;
            if (request.Email != null) user.Email = request.Email.ToLower();
            if (request.Phone != null) user.Phone = request.Phone;
            if (request.JobTitle != null) user.JobTitle = request.JobTitle;
            if (request.JoinDate.HasValue) user.JoinDate = request.JoinDate;
            if (request.RoleId.HasValue) user.RoleId = request.RoleId;
            if (request.StatusId.HasValue) user.StatusId = request.StatusId;
            if (request.O365Id != null) user.O365Id = request.O365Id;
            if (request.IsExternal.HasValue) user.IsExternal = request.IsExternal.Value;
            if (request.IsSynced.HasValue) user.IsSynced = request.IsSynced.Value;
            if (request.DisplayName != null) user.DisplayName = request.DisplayName;
            if (request.Gender != null) user.Gender = request.Gender;
            if (request.Country != null) user.Country = request.Country;
            if (request.State != null) user.State = request.State;
            if (request.Language != null) user.Language = request.Language;
            if (request.Timezone != null) user.Timezone = request.Timezone;
            if (request.ManagerEmail != null) user.ManagerEmail = request.ManagerEmail;

            if (request.SkillIds != null)
            {
                user.Skills.Clear();
                var skills = await _context.Skills
                    .Where(s => request.SkillIds.Contains(s.Id))
                    .ToListAsync();
                foreach (var skill in skills)
                {
                    user.Skills.Add(skill);
                }
            }

            await _context.SaveChangesAsync();
            // Role / external flag / status may have changed what this person may see in chat.
            await _chatAccess.RevalidateUserAsync(id);
            return MapToUserDto(user);
        }, nameof(UpdateUserAsync));
    }

    public async Task<bool> DeleteUserAsync(int id, string actorId)
    {
        return await ExecuteAsync(async () =>
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return false;

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            await _chatAccess.RevalidateUserAsync(id);
            return true;
        }, nameof(DeleteUserAsync));
    }

    public async Task<IEnumerable<UserResponseDto>> SearchUsersAsync(string? query, int limit, bool? isActive)
    {
        return await ExecuteAsync(async () =>
        {
            var queryable = _context.Users
                .Include(u => u.Role)
                .Include(u => u.Status)
                .Include(u => u.Manager)
                .Include(u => u.Skills)
                .AsQueryable();

            if (!string.IsNullOrEmpty(query))
            {
                var searchLower = query.ToLower();
                queryable = queryable.Where(u => u.FirstName.ToLower().Contains(searchLower) ||
                                                 (u.LastName != null && u.LastName.ToLower().Contains(searchLower)) ||
                                                 u.Email.ToLower().Contains(searchLower) ||
                                                 u.Username.ToLower().Contains(searchLower) ||
                                                 (u.DisplayName != null && u.DisplayName.ToLower().Contains(searchLower)));
            }

            if (isActive.HasValue)
            {
                queryable = queryable.Where(u => u.IsActive == isActive.Value);
            }

            var items = await queryable
                .OrderBy(u => u.Id)
                .Take(limit)
                .ToListAsync();

            return items.Select(MapToUserDto);
        }, nameof(SearchUsersAsync));
    }

    // Bulk-pulls every user in the Azure AD tenant via Microsoft Graph and upserts each one
    // locally. Separate code path from AuthService.SyncUserAsync (the per-login sync) on
    // purpose: that one runs *as* the logging-in user with a delegated token scoped to
    // their own profile (User.Read); this one needs an app-only token with the *application*
    // permission User.Read.All (admin-consented in Azure AD — a Graph API/Entra config step,
    // not something this code can grant itself) to list the whole directory.
    // App-only token (client credentials grant) for calling Microsoft Graph as the app
    // itself, not as a signed-in user — needs the 'User.Read.All' *application* permission
    // admin-consented in Azure AD. Shared by both the bulk directory sync and the
    // Graph-backed user search (autocomplete for users who haven't logged in yet).
    private async Task<string> GetAppOnlyGraphTokenAsync()
    {
        var tenantId = _configuration["AzureAd:TenantId"];
        var clientId = _configuration["AzureAd:ClientId"];
        var clientSecret = _configuration["AzureAd:ClientSecret"];

        if (string.IsNullOrEmpty(tenantId) || string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
        {
            throw new InvalidOperationException("Microsoft SSO configuration is incomplete on this server.");
        }

        var tokenUrl = $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token";
        var tokenParams = new Dictionary<string, string>
        {
            { "grant_type", "client_credentials" },
            { "client_id", clientId },
            { "client_secret", clientSecret },
            { "scope", "https://graph.microsoft.com/.default" },
        };

        HttpResponseMessage tokenResponse;
        try
        {
            tokenResponse = await _httpClient.PostAsync(tokenUrl, new FormUrlEncodedContent(tokenParams));
            tokenResponse.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "Failed to acquire an app-only Microsoft Graph token. Most likely cause: " +
                "the 'User.Read.All' *application* permission hasn't been granted admin " +
                $"consent for this app registration in Azure AD. Details: {ex.Message}");
        }

        var tokenJson = JsonNode.Parse(await tokenResponse.Content.ReadAsStringAsync());
        var graphToken = tokenJson?["access_token"]?.ToString();
        if (string.IsNullOrEmpty(graphToken))
        {
            throw new InvalidOperationException("Microsoft did not return an access token for the Graph sync.");
        }
        return graphToken;
    }

    public async Task<SyncUsersResponse> SyncUsersFromAzureAsync()
    {
        return await ExecuteAsync(async () =>
        {
            var graphToken = await GetAppOnlyGraphTokenAsync();

            // 2. Page through every user in the tenant.
            var graphUsers = new List<JsonNode>();
            var nextUrl = "https://graph.microsoft.com/v1.0/users" +
                "?$select=id,mail,userPrincipalName,givenName,surname,displayName,accountEnabled&$top=999";

            while (!string.IsNullOrEmpty(nextUrl))
            {
                var request = new HttpRequestMessage(HttpMethod.Get, nextUrl);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", graphToken);

                HttpResponseMessage pageResponse;
                try
                {
                    pageResponse = await _httpClient.SendAsync(request);
                    pageResponse.EnsureSuccessStatusCode();
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Microsoft Graph rejected the directory listing request: {ex.Message}");
                }

                var pageJson = JsonNode.Parse(await pageResponse.Content.ReadAsStringAsync());
                foreach (var u in pageJson?["value"]?.AsArray() ?? new JsonArray())
                {
                    if (u != null) graphUsers.Add(u);
                }

                nextUrl = pageJson?["@odata.nextLink"]?.ToString() ?? string.Empty;
            }

            // 3. Upsert each one — same match-by-O365Id-then-email + "only assign defaults
            // when unset" rule as AuthService.SyncUserAsync, so a pre-provisioned role never
            // gets clobbered by a bulk sync that runs after it.
            var defaultRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Employee");
            var defaultStatus = await _context.UserStatuses.FirstOrDefaultAsync(s => s.Name == "Active");

            int created = 0, updated = 0, skipped = 0;

            foreach (var gu in graphUsers)
            {
                var o365Id = gu["id"]?.ToString();
                var email = (gu["mail"]?.ToString() ?? gu["userPrincipalName"]?.ToString())?.ToLower();
                var firstName = gu["givenName"]?.ToString() ?? "";
                var lastName = gu["surname"]?.ToString() ?? "";
                var displayName = gu["displayName"]?.ToString();

                if (string.IsNullOrEmpty(o365Id) || string.IsNullOrEmpty(email))
                {
                    skipped++;
                    continue;
                }

                var existing = await _context.Users.FirstOrDefaultAsync(u => u.O365Id == o365Id || u.Email == email);

                if (existing != null)
                {
                    existing.O365Id = o365Id;
                    existing.IsSynced = true;
                    if (!string.IsNullOrEmpty(firstName)) existing.FirstName = firstName;
                    existing.LastName = lastName;
                    existing.DisplayName = displayName ?? $"{firstName} {lastName}".Trim();
                    if (existing.RoleId == null) existing.RoleId = defaultRole?.Id;
                    if (existing.StatusId == null) existing.StatusId = defaultStatus?.Id;
                    updated++;
                }
                else
                {
                    var baseUsername = email.Split('@')[0].ToLower().Replace(".", "_");
                    var username = baseUsername;
                    if (await _context.Users.AnyAsync(u => u.Username == username))
                    {
                        username = $"{baseUsername}_{Guid.NewGuid().ToString("N").Substring(0, 8)}";
                    }

                    _context.Users.Add(new User
                    {
                        PublicId = "USR-" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper(),
                        EmployeeId = "EMP-" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper(),
                        FirstName = string.IsNullOrEmpty(firstName) ? email.Split('@')[0] : firstName,
                        LastName = lastName,
                        Email = email,
                        Username = username,
                        DisplayName = displayName ?? $"{firstName} {lastName}".Trim(),
                        O365Id = o365Id,
                        IsSynced = true,
                        IsExternal = false,
                        RoleId = defaultRole?.Id,
                        StatusId = defaultStatus?.Id,
                    });
                    created++;
                }
            }

            await _context.SaveChangesAsync();

            return new SyncUsersResponse(graphUsers.Count, created, updated, skipped);
        }, nameof(SyncUsersFromAzureAsync));
    }

    // Backs GET /graph/search-users — typeahead search across the whole Azure AD tenant,
    // not just users already synced locally. Lets an admin add someone to a project/team
    // before that person has ever logged into this app. Same app-only token requirement as
    // the bulk sync above; if that permission isn't consented yet this throws the same
    // clear error rather than returning an empty result that looks like "no matches".
    public async Task<IEnumerable<AzureUserSearchResult>> SearchAzureUsersAsync(string query, int limit)
    {
        return await ExecuteAsync(async () =>
        {
            if (string.IsNullOrWhiteSpace(query)) return Enumerable.Empty<AzureUserSearchResult>();

            var graphToken = await GetAppOnlyGraphTokenAsync();

            // OData string literals delimit with single quotes — escape any in the query by
            // doubling them (the standard OData escape), so a query can't break out of the
            // filter expression or otherwise malform it.
            var safe = query.Replace("'", "''");
            var filter = $"startswith(displayName,'{safe}') or startswith(mail,'{safe}') or startswith(userPrincipalName,'{safe}')";
            var url = "https://graph.microsoft.com/v1.0/users" +
                $"?$filter={Uri.EscapeDataString(filter)}" +
                "&$select=id,displayName,mail,userPrincipalName,givenName,surname" +
                $"&$top={Math.Clamp(limit, 1, 50)}";

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", graphToken);

            HttpResponseMessage response;
            try
            {
                response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Microsoft Graph rejected the user search request: {ex.Message}");
            }

            var json = JsonNode.Parse(await response.Content.ReadAsStringAsync());
            var results = new List<AzureUserSearchResult>();
            foreach (var u in json?["value"]?.AsArray() ?? new JsonArray())
            {
                if (u == null) continue;
                results.Add(new AzureUserSearchResult(
                    u["id"]?.ToString() ?? "",
                    u["displayName"]?.ToString(),
                    u["mail"]?.ToString(),
                    u["userPrincipalName"]?.ToString(),
                    u["givenName"]?.ToString(),
                    u["surname"]?.ToString()
                ));
            }
            return results;
        }, nameof(SearchAzureUsersAsync));
    }

    private UserResponseDto MapToUserDto(User user)
    {
        RoleResponseDto? roleDto = null;
        if (user.Role != null)
        {
            JsonNode? permsNode = null;
            try { permsNode = JsonNode.Parse(user.Role.Permissions ?? "{}"); } catch { permsNode = JsonNode.Parse("{}"); }
            roleDto = new RoleResponseDto(user.Role.Id, user.Role.Name, permsNode);
        }

        MasterResponse? statusDto = null;
        if (user.Status != null)
        {
            statusDto = new MasterResponse(user.Status.Id, user.Status.Name);
        }

        UserResponseBaseDto? managerDto = null;
        if (user.Manager != null)
        {
            managerDto = new UserResponseBaseDto(
                user.Manager.Id,
                user.Manager.PublicId,
                user.Manager.Email,
                user.Manager.Username,
                user.Manager.FirstName,
                user.Manager.LastName,
                user.Manager.DisplayName,
                user.Manager.IsExternal,
                user.Manager.IsSynced
            );
        }

        var skillsList = user.Skills?.Select(s => new SkillResponse(s.Id, s.Name)) ?? Enumerable.Empty<SkillResponse>();

        return new UserResponseDto(
            user.Id,
            user.PublicId,
            user.EmployeeId,
            user.FirstName,
            user.LastName,
            user.Email,
            user.Username,
            user.Phone,
            user.JobTitle,
            user.JoinDate,
            user.RoleId,
            user.StatusId,
            user.ManagerEmail,
            user.O365Id,
            user.DisplayName,
            user.Gender,
            user.Country,
            user.State,
            user.Language,
            user.Timezone,
            user.IsExternal,
            user.IsSynced,
            roleDto,
            statusDto,
            managerDto,
            skillsList
        );
    }
}
