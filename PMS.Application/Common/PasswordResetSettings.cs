namespace PMS.Application.Common;

/// <summary>Where the "set a new password" link in the e-mail points (the PMS web app's address).</summary>
public sealed record PasswordResetSettings(string FrontendBaseUrl);
