using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;

namespace Techspear.Identity.Adapter;

/// <summary>
/// Reads a Spear ID access token. Each app keeps its own user id and re-issues the claims it already understands.
/// </summary>
public static class SpearIdTokens
{
    public const string DefaultIssuer = "techspear-identity";

    public static bool IsHubBearer(string? authorizationHeader, string? issuer = null)
    {
        var token = Bearer(authorizationHeader);
        if (token is null) return false;
        try
        {
            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
            var expected = string.IsNullOrWhiteSpace(issuer) ? DefaultIssuer : issuer;
            return string.Equals(jwt.Issuer, expected, StringComparison.Ordinal);
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    public static SpearIdentity? Read(string? authorizationHeader, string signingKey, string? issuer = null, string? audience = null)
    {
        var token = Bearer(authorizationHeader);
        if (token is null || string.IsNullOrWhiteSpace(signingKey)) return null;

        var expectedIssuer = string.IsNullOrWhiteSpace(issuer) ? DefaultIssuer : issuer;
        try
        {
            var principal = new JwtSecurityTokenHandler { MapInboundClaims = false }.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = expectedIssuer,
                ValidateAudience = !string.IsNullOrWhiteSpace(audience),
                ValidAudience = audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
                ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(1)
            }, out _);

            var sub = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            if (!Guid.TryParse(sub, out var subject)) return null;

            return new SpearIdentity
            {
                Subject = subject,
                Email = principal.FindFirst(JwtRegisteredClaimNames.Email)?.Value ?? "",
                Name = principal.FindFirst(JwtRegisteredClaimNames.Name)?.Value ?? "",
                UserType = principal.FindFirst("utype")?.Value ?? "",
                Role = principal.FindFirst("role")?.Value ?? "",
                Permissions = ReadPermissions(principal)
            };
        }
        catch (Exception ex) when (ex is SecurityTokenException or ArgumentException)
        {
            return null;
        }
    }

    public static string? Bearer(string? authorizationHeader)
    {
        if (string.IsNullOrWhiteSpace(authorizationHeader)) return null;
        const string prefix = "Bearer ";
        if (!authorizationHeader.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return null;
        var token = authorizationHeader[prefix.Length..].Trim();
        return token.Count(c => c == '.') == 2 ? token : null;
    }

    private static IReadOnlyList<string> ReadPermissions(ClaimsPrincipal principal)
    {
        var values = principal.FindAll("perm").Select(c => c.Value).ToList();
        if (values.Count == 1 && values[0].TrimStart().StartsWith('['))
        {
            try
            {
                return JsonSerializer.Deserialize<List<string>>(values[0]) ?? [];
            }
            catch (JsonException)
            {
                return [];
            }
        }
        return values;
    }
}

public sealed class SpearIdentity
{
    public Guid Subject { get; init; }
    public string Email { get; init; } = "";
    public string Name { get; init; } = "";
    public string UserType { get; init; } = "";
    public string Role { get; init; } = "";
    public IReadOnlyList<string> Permissions { get; init; } = [];
}
