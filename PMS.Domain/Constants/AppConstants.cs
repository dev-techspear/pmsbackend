namespace PMS.Domain.Constants;

public static class AppConstants
{
    /// <summary>SQL Server schema that owns every PMS table in the shared techspear database.</summary>
    public const string PmsSchema = "pms";

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
    public const string PermUserView = "user-view";
    public const string PermUserCreate = "user-create";
    public const string PermUserEdit = "user-edit";
    public const string PermUserDelete = "user-delete";
    public const string PermAutomationManage = "automation-manage";
    public const string PermAuditView = "audit-view";

    // Project chat
    public const int ChatMaxBodyLength = 4000;
    public const int ChatDefaultPageSize = 50;
    public const int ChatMaxPageSize = 100;
    // Project documents (uploaded to blob storage under DocumentBlobPrefix, one shared container)
    public const string DocumentBlobPrefix = "pms/documents";
    public const int DocumentMaxBytes = 25 * 1024 * 1024;
    public const int DocumentSasMinutes = 10;
    public static readonly string[] DocumentAllowedExtensions =
    [
        ".png", ".jpg", ".jpeg", ".gif", ".webp",
        ".pdf", ".txt", ".csv", ".md", ".json",
        ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".zip",
    ];

    // Forgot password (external users with a local password)
    public const int PasswordResetTokenMinutes = 60;
    public const int PasswordResetMaxRequestsPerHour = 3;
    public const string PasswordResetSentMessage =
        "If that email belongs to a PMS account, we have sent a link to set a new password. It works once and expires in 60 minutes.";
    public const string PasswordResetInvalidLinkMessage =
        "This password reset link is invalid or has expired. Request a new one.";
    public const string PasswordResetUnavailableMessage =
        "Password reset by email is not available yet. Please contact support@techspear.in.";
    public const string MailProfileDefault = "noreply";
    // Templates held in the Techspear Mail Service (edited there, in the dashboard Templates page).
    public const string MailTemplatePasswordReset = "pms.account.password.reset";
    public const string MailTemplatePasswordChanged = "pms.account.password.changed";
    // Shown in the greeting when a person has no first name ("Hi there,").
    public const string MailGreetingFallback = "there";
    public const string SessionEndedMessage = "Your session has ended. Please sign in again.";

    // Tasks: a task and its sub-tasks. A sub-task cannot have sub-tasks of its own.
    public const int MaxTaskDepth = 2;
    public const string TaskStatusCompleted = "completed";

    // Project links & notes
    public static readonly string[] LinkTypes = ["Repo", "Live", "Staging", "Design", "Docs", "Other"];
    public static readonly string[] LinkAllowedSchemes = ["http", "https"];
    public const int LinkMaxUrlLength = 2000;
    public const int NoteMaxBodyLength = 10000;

    // Project credentials
    public static readonly string[] CredentialTypes = ["Server", "Database", "ApiKey", "Git", "Cloud", "Application", "Other"];
    public const string CredentialActionReveal = "REVEAL";
    public const string CredentialActionCreate = "CREATE";
    public const string CredentialActionRotate = "ROTATE";
    public const string CredentialRevealRateLimitPolicy = "credential-reveal";
    public const int CredentialRevealRateLimitPermits = 20;
    public const int CredentialRevealRateLimitWindowMinutes = 1;

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
    // How long a minted attachment-download SAS URL stays valid - long enough for one browser
    // fetch, short enough that a leaked URL (e.g. in a proxy log) is useless soon after.
    public const int ChatAttachmentSasMinutes = 10;

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
