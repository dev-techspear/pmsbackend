namespace PMS.Application.Common.Interfaces;

/// <summary>
/// Called after a change that can take someone's access to a chat room away (member removed, project
/// deleted, user deactivated, role permissions edited) so live connections are re-checked at once.
/// </summary>
public interface IChatAccessSync
{
    Task RevalidateProjectAsync(int projectId);
    Task RevalidateUserAsync(int userId);
    Task RevalidateAllAsync();
}

/// <summary>Default for hosts without a real-time hub (tests, tools): nothing to re-check.</summary>
public sealed class NullChatAccessSync : IChatAccessSync
{
    public Task RevalidateProjectAsync(int projectId) => Task.CompletedTask;
    public Task RevalidateUserAsync(int userId) => Task.CompletedTask;
    public Task RevalidateAllAsync() => Task.CompletedTask;
}
