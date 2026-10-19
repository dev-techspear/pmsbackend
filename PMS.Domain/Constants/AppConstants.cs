namespace PMS.Domain.Constants;

public static class AppConstants
{
    public const string RoleAdmin = "Admin";
    public const string RoleEmployee = "Employee";
    public const string RoleFreelancer = "Freelancer";

    public const string UserStatusActive = "Active";
    public const string UserStatusPending = "Pending";

    // Microsoft Graph `userType` value for B2B guests.
    public const string GraphUserTypeGuest = "Guest";

    // Password policy for external (email + password) accounts.
    public const int PasswordMinLength = 10;
    public const int PasswordMaxLength = 128;
    public const int PasswordHashIterations = 600_000;
    public const int PasswordSaltBytes = 16;
    public const int PasswordKeyBytes = 32;
    public const string PasswordHashPrefix = "PBKDF2-SHA256";

    // Per-IP rate limit for the anonymous login/register endpoints.
    public const string AuthRateLimitPolicy = "auth";
    public const int AuthRateLimitPermits = 10;
    public const int AuthRateLimitWindowMinutes = 1;

    // View-permission levels stored on a role ("O"/"A" are the dropdown values in role config).
    public const string ViewLevelAll = "All";
    public const string ViewLevelOwned = "O";
    public const string ViewLevelAssociated = "A";

    // Permission ids (must match the frontend's availablePermissions).
    public const string PermProjectView = "proj-view";
    public const string PermProjectCreate = "proj-create";
    public const string PermProjectEdit = "proj-edit";
    public const string PermProjectDelete = "proj-delete";
    public const string PermTaskView = "task-view";
    public const string PermIssueView = "issue-view";
    public const string PermMilestoneView = "milestone-view";
    public const string PermTimeView = "time-view";
    public const string PermChatView = "chat-view";
    public const string PermChatPost = "chat-post";

    // Project chat
    public const int ChatMaxBodyLength = 4000;
    public const int ChatDefaultPageSize = 50;
    public const int ChatMaxPageSize = 100;
    // Attachments
    public const int ChatMaxAttachments = 5;
    public const int ChatMaxAttachmentBytes = 10 * 1024 * 1024;
    public static readonly string[] ChatAllowedExtensions =
    [
        ".png", ".jpg", ".jpeg", ".gif", ".webp",
        ".pdf", ".txt", ".csv", ".md",
        ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".zip",
    ];
    // Shown inline as a preview; everything else is download-only.
    public static readonly string[] ChatInlineImageExtensions = [".png", ".jpg", ".jpeg", ".gif", ".webp"];

    // Short-lived credential for the chat WebSocket (a URL can end up in logs, so it must expire fast).
    public const string ChatTicketClaimType = "type";
    public const string ChatTicketClaimValue = "chat-ticket";
    public const int ChatTicketLifetimeSeconds = 60;
    public const string ChatHubPath = "/hubs/chat";
    public const string ChatRateLimitPolicy = "chat";
    public const int ChatRateLimitPermits = 30;
    public const int ChatRateLimitWindowMinutes = 1;

    public const string InvalidCredentialsMessage = "Invalid email or password.";
    public const string RegistrationReceivedMessage =
        "Registration received. An administrator must approve your account before you can sign in.";
    public const string AccountPendingMessage =
        "Your account is awaiting administrator approval.";
}
