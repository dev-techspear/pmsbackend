using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using PMS.Application.Common.Interfaces;
using PMS.Domain.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace PMS.Infrastructure.Services;

public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;

    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateAccessToken(User user)
    {
        var secretKey = _configuration["SECRET_KEY"] ?? "0ad7e425b1d48f0cefc36120b071b4b8f078e7d0689b2b0a001b430f2626799a";
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var expiryMinutesStr = _configuration["ACCESS_TOKEN_EXPIRE_MINUTES"] ?? "30";
        _ = double.TryParse(expiryMinutesStr, out double expiryMinutes);
        if (expiryMinutes <= 0) expiryMinutes = 30;

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role?.Name ?? "Employee")
        };

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken(User user)
    {
        var secretKey = _configuration["SECRET_KEY"] ?? "0ad7e425b1d48f0cefc36120b071b4b8f078e7d0689b2b0a001b430f2626799a";
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim("type", "refresh")
        };

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateChatTicket(int userId)
    {
        var secretKey = _configuration["SECRET_KEY"] ?? "0ad7e425b1d48f0cefc36120b071b4b8f078e7d0689b2b0a001b430f2626799a";
        var creds = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)), SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            claims: new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(PMS.Domain.Constants.AppConstants.ChatTicketClaimType, PMS.Domain.Constants.AppConstants.ChatTicketClaimValue),
            },
            expires: DateTime.UtcNow.AddSeconds(PMS.Domain.Constants.AppConstants.ChatTicketLifetimeSeconds),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        var secretKey = _configuration["SECRET_KEY"] ?? "0ad7e425b1d48f0cefc36120b071b4b8f078e7d0689b2b0a001b430f2626799a";
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = false,
            ValidateIssuer = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ValidateLifetime = false
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);
        if (securityToken is not JwtSecurityToken jwtSecurityToken || 
            !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
        {
            throw new SecurityTokenException("Invalid token algorithm");
        }

        return principal;
    }
}
