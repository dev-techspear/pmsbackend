using System.Collections.Concurrent;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using PMS.Application.Common.Interfaces;
using PMS.Application.Services.Interfaces;

namespace PMS.API.Hubs;

/// <summary>Who is connected and which rooms each connection has joined - needed to kick people out when access changes.</summary>
public sealed class ChatConnectionRegistry
{
    private sealed record State(int UserId, HashSet<int> Projects);

    private readonly ConcurrentDictionary<string, State> _connections = new();

    public void Joined(string connectionId, int userId, int projectId)
    {
        var state = _connections.GetOrAdd(connectionId, _ => new State(userId, new HashSet<int>()));
        lock (state.Projects) state.Projects.Add(projectId);
    }

    public void Left(string connectionId, int projectId)
    {
        if (_connections.TryGetValue(connectionId, out var state))
            lock (state.Projects) state.Projects.Remove(projectId);
    }

    public void Disconnected(string connectionId) => _connections.TryRemove(connectionId, out _);

    /// <summary>Snapshot of (connection, user, project) for rooms matching the filter.</summary>
    public IReadOnlyList<(string ConnectionId, int UserId, int ProjectId)> Memberships(
        Func<int, int, bool>? userProjectFilter = null)
    {
        var result = new List<(string, int, int)>();
        foreach (var (connectionId, state) in _connections)
        {
            int[] projects;
            lock (state.Projects) projects = state.Projects.ToArray();
            foreach (var projectId in projects)
                if (userProjectFilter == null || userProjectFilter(state.UserId, projectId))
                    result.Add((connectionId, state.UserId, projectId));
        }
        return result;
    }
}

/// <summary>
/// Real-time channel for project chat. Clients only join/leave rooms here; messages are written
/// through the REST API (so every write is validated and saved first) and then pushed to the
/// room's group by <see cref="Controllers.ChatController"/>.
/// </summary>
[Authorize]
public class ChatHub : Hub
{
    public static string Group(int projectId) => $"project-{projectId}";

    private readonly IChatService _chat;
    private readonly ChatConnectionRegistry _registry;

    public ChatHub(IChatService chat, ChatConnectionRegistry registry)
    {
        _chat = chat;
        _registry = registry;
    }

    private int UserId =>
        int.TryParse(Context.User?.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
            ? id
            : throw new HubException("Not signed in.");

    public async System.Threading.Tasks.Task JoinProject(int projectId)
    {
        if (!await _chat.CanAccessAsync(projectId, UserId))
            throw new HubException("Access denied.");
        await Groups.AddToGroupAsync(Context.ConnectionId, Group(projectId));
        _registry.Joined(Context.ConnectionId, UserId, projectId);
    }

    public System.Threading.Tasks.Task LeaveProject(int projectId)
    {
        _registry.Left(Context.ConnectionId, projectId);
        return Groups.RemoveFromGroupAsync(Context.ConnectionId, Group(projectId));
    }

    public override System.Threading.Tasks.Task OnDisconnectedAsync(Exception? exception)
    {
        _registry.Disconnected(Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }
}

/// <summary>
/// Re-checks live connections after something changes who may see a room, and removes anyone who
/// no longer qualifies (they are also told, so the page can say why).
/// </summary>
public sealed class ChatAccessSync : IChatAccessSync
{
    private readonly IHubContext<ChatHub> _hub;
    private readonly ChatConnectionRegistry _registry;
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<ChatAccessSync> _logger;

    public ChatAccessSync(IHubContext<ChatHub> hub, ChatConnectionRegistry registry,
        IServiceScopeFactory scopes, ILogger<ChatAccessSync> logger)
    {
        _hub = hub;
        _registry = registry;
        _scopes = scopes;
        _logger = logger;
    }

    public Task RevalidateProjectAsync(int projectId) => RevalidateAsync((_, p) => p == projectId);
    public Task RevalidateUserAsync(int userId) => RevalidateAsync((u, _) => u == userId);
    public Task RevalidateAllAsync() => RevalidateAsync(null);

    private async Task RevalidateAsync(Func<int, int, bool>? filter)
    {
        try
        {
            var candidates = _registry.Memberships(filter);
            if (candidates.Count == 0) return;

            // One scope per user+project check, so a stale cached permission can never mask a change.
            foreach (var (connectionId, userId, projectId) in candidates)
            {
                using var scope = _scopes.CreateScope();
                var chat = scope.ServiceProvider.GetRequiredService<IChatService>();
                if (await chat.CanAccessAsync(projectId, userId)) continue;

                _registry.Left(connectionId, projectId);
                await _hub.Groups.RemoveFromGroupAsync(connectionId, ChatHub.Group(projectId));
                await _hub.Clients.Client(connectionId).SendAsync("AccessRevoked", new { projectId });
            }
        }
        catch (Exception ex)
        {
            // Never let a chat clean-up fail the change (removing a member, editing a role) that triggered it.
            _logger.LogError(ex, "Chat access re-check failed");
        }
    }
}
