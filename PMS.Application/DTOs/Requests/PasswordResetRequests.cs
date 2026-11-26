namespace PMS.Application.DTOs.Requests;

public record ForgotPasswordRequest(string? Email);

public record ResetPasswordRequest(string? Token, string? NewPassword);
