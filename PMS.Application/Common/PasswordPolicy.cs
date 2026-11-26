using PMS.Domain.Constants;

namespace PMS.Application.Common;

/// <summary>The one password rule set, used by registration and by password reset.</summary>
public static class PasswordPolicy
{
    public static IEnumerable<string> Validate(string? password)
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
}
