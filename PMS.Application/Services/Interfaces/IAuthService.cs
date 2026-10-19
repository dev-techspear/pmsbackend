using PMS.Application.DTOs.Requests;
using PMS.Application.DTOs.Responses;

namespace PMS.Application.Services.Interfaces;

public interface IAuthService
{
    Task<TokenResponse> HandleMicrosoftCallbackAsync(MSCallbackRequest payload);
    Task<TokenResponse> LoginExternalAsync(LoginRequest payload);
    Task<RegisterResponse> RegisterExternalAsync(RegisterRequest payload);
    Task<TokenResponse> RefreshTokenAsync(RefreshTokenRequest payload);
    Task<UserProfileResponse?> GetUserProfileAsync(int userId);
}
