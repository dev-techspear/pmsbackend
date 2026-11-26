namespace PMS.Application.Services.Interfaces;

public enum ForgotPasswordOutcome
{
    /// <summary>Handled. The caller always shows the same message, whether or not the e-mail matched an account.</summary>
    Accepted,
    /// <summary>No mail service is configured here, so no reset e-mail can be sent.</summary>
    Unavailable,
}

public interface IPasswordResetService
{
    System.Threading.Tasks.Task<ForgotPasswordOutcome> RequestAsync(string? email);

    /// <summary>Throws InvalidOperationException with a user-safe message when the link or password is not acceptable.</summary>
    System.Threading.Tasks.Task ResetAsync(string? token, string? newPassword);
}
