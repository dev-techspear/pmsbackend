namespace PMS.Application.DTOs.Responses;

public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiry,
    UserDto User);
