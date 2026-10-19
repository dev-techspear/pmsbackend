using PMS.Domain.Entities;
using System.Security.Claims;

namespace PMS.Application.Common.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken(User user);

    /// <summary>A token good for about a minute that only the chat hub accepts.</summary>
    string GenerateChatTicket(int userId);
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}
