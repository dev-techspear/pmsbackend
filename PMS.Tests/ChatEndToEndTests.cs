using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PMS.Application.Common.Interfaces;
using PMS.Domain.Entities;
using PMS.Infrastructure.Data;
using Task = System.Threading.Tasks.Task;

namespace PMS.Tests;

/// <summary>
/// Drives the REAL app (auth, filters, rate limiter, controller, SignalR hub) in memory: the
/// wiring unit tests cannot see - the token sent as ?access_token for WebSockets, who may join a
/// room, and the broadcast after a REST post.
/// </summary>
public class ChatEndToEndTests : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;

    private User _member = null!, _stranger = null!, _freelancerNoChat = null!, _freelancerChat = null!;
    private Project _project = null!;

    public ChatEndToEndTests()
    {
        var dbName = Guid.NewGuid().ToString();
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, cfg) => cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Server=unused;Database=unused;",
            }));
            builder.ConfigureServices(services =>
            {
                services.RemoveAll(typeof(IDbContextOptionsConfiguration<ApplicationDbContext>));
                services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
                services.AddDbContext<ApplicationDbContext>(o => o.UseInMemoryDatabase(dbName));
            });
        });
        Seed().GetAwaiter().GetResult();
    }

    public void Dispose() => _factory.Dispose();

    private async Task Seed()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        // The app's own startup seeding already ran (roles, statuses); reuse what it created.
        var employee = await db.Roles.FirstAsync(r => r.Name == "Employee");
        var freelancer = await db.Roles.FirstAsync(r => r.Name == "Freelancer");
        var active = await db.UserStatuses.FirstAsync(s => s.Name == "Active");

        var noChat = new Role { Name = "FreelancerNoChat", Permissions = """{"proj-view":"A"}""" };
        var withChat = new Role { Name = "FreelancerChat", Permissions = """{"proj-view":"A","chat-view":true,"chat-post":true}""" };
        db.Roles.AddRange(noChat, withChat);
        await db.SaveChangesAsync();

        _member = TestSupport.NewUser("member@e2e.com", employee.Id, active.Id);
        _stranger = TestSupport.NewUser("stranger@e2e.com", employee.Id, active.Id);
        _freelancerNoChat = TestSupport.NewUser("nochat@e2e.com", noChat.Id, active.Id, external: true);
        _freelancerChat = TestSupport.NewUser("chat@e2e.com", withChat.Id, active.Id, external: true);
        db.Users.AddRange(_member, _stranger, _freelancerNoChat, _freelancerChat);
        await db.SaveChangesAsync();

        _project = TestSupport.NewProject("E2E room", ownerId: _member.Id);
        _project.TeamMembers.Add(new ProjectMember { UserId = _freelancerNoChat.Id });
        _project.TeamMembers.Add(new ProjectMember { UserId = _freelancerChat.Id });
        db.Projects.Add(_project);
        await db.SaveChangesAsync();
    }

    private string TokenFor(User u)
    {
        using var scope = _factory.Services.CreateScope();
        var tokens = scope.ServiceProvider.GetRequiredService<ITokenService>();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return tokens.GenerateAccessToken(db.Users.Include(x => x.Role).First(x => x.Id == u.Id));
    }

    private HttpClient ClientFor(User u)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TokenFor(u));
        return client;
    }

    private HubConnection HubFor(User? u)
    {
        return new HubConnectionBuilder()
            .WithUrl(new Uri(_factory.Server.BaseAddress, "/hubs/chat"), o =>
            {
                o.Transports = HttpTransportType.LongPolling;       // works with the in-memory TestServer
                o.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
                if (u != null) o.AccessTokenProvider = () => Task.FromResult<string?>(TokenFor(u));
            })
            .Build();
    }

    private string MessagesUrl => $"/api/v1/projects/{_project.Id}/chat/messages";

    [Fact]
    public async Task A_member_receives_a_live_message_posted_over_REST_in_the_same_shape_as_the_REST_API()
    {
        var received = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var hub = HubFor(_member);
        hub.On<JsonElement>("MessageReceived", m => received.TrySetResult(m));
        await hub.StartAsync();
        await hub.InvokeAsync("JoinProject", _project.Id);

        var post = await ClientFor(_freelancerChat).PostAsJsonAsync(MessagesUrl, new { body = "hello from outside" });
        Assert.Equal(HttpStatusCode.Created, post.StatusCode);

        var pushed = await received.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal("hello from outside", pushed.GetProperty("body").GetString());
        Assert.True(pushed.GetProperty("sender_is_external").GetBoolean());     // snake_case, like REST
        Assert.Equal(_project.Id, pushed.GetProperty("project_id").GetInt32());

        var history = await ClientFor(_member).GetFromJsonAsync<JsonElement>(MessagesUrl);
        Assert.Equal("hello from outside", history.GetProperty("data")[0].GetProperty("body").GetString());
    }

    [Fact]
    public async Task A_non_member_cannot_join_the_room_so_never_hears_its_messages()
    {
        await using var hub = HubFor(_stranger);
        await hub.StartAsync();
        var ex = await Assert.ThrowsAsync<HubException>(() => hub.InvokeAsync("JoinProject", _project.Id));
        Assert.Contains("Access denied", ex.Message);
    }

    [Fact]
    public async Task A_connection_without_a_token_is_refused()
    {
        await using var hub = HubFor(null);
        await Assert.ThrowsAnyAsync<Exception>(() => hub.StartAsync());
    }

    [Fact]
    public async Task A_non_member_gets_403_from_the_REST_endpoints()
    {
        var client = ClientFor(_stranger);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(MessagesUrl)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await client.PostAsJsonAsync(MessagesUrl, new { body = "let me in" })).StatusCode);
    }

    [Fact]
    public async Task An_external_member_whose_role_lacks_chat_permissions_is_refused_everywhere()
    {
        var client = ClientFor(_freelancerNoChat);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(MessagesUrl)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/v1/chat/unread")).StatusCode);

        await using var hub = HubFor(_freelancerNoChat);
        await hub.StartAsync();
        await Assert.ThrowsAsync<HubException>(() => hub.InvokeAsync("JoinProject", _project.Id));
    }

    [Fact]
    public async Task The_mention_member_list_follows_the_same_access_rules()
    {
        var url = $"/api/v1/projects/{_project.Id}/chat/members";
        var ok = await ClientFor(_freelancerChat).GetFromJsonAsync<JsonElement>(url);
        Assert.True(ok.GetProperty("data").GetArrayLength() >= 3);
        Assert.Equal(HttpStatusCode.Forbidden, (await ClientFor(_stranger).GetAsync(url)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await ClientFor(_freelancerNoChat).GetAsync(url)).StatusCode);
    }

    [Fact]
    public async Task Requests_without_a_token_get_401()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await _factory.CreateClient().GetAsync(MessagesUrl)).StatusCode);
    }

    [Fact]
    public async Task Editing_and_deleting_are_pushed_to_the_room_and_validation_errors_are_400()
    {
        var edited = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
        var deleted = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var hub = HubFor(_member);
        hub.On<JsonElement>("MessageEdited", m => edited.TrySetResult(m));
        hub.On<JsonElement>("MessageDeleted", m => deleted.TrySetResult(m));
        await hub.StartAsync();
        await hub.InvokeAsync("JoinProject", _project.Id);

        var me = ClientFor(_member);
        var created = await (await me.PostAsJsonAsync(MessagesUrl, new { body = "draft" })).Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("data").GetProperty("id").GetInt32();

        Assert.Equal(HttpStatusCode.OK, (await me.PutAsJsonAsync($"{MessagesUrl}/{id}", new { body = "final" })).StatusCode);
        Assert.Equal("final", (await edited.Task.WaitAsync(TimeSpan.FromSeconds(10))).GetProperty("body").GetString());

        Assert.Equal(HttpStatusCode.BadRequest, (await me.PostAsJsonAsync(MessagesUrl, new { body = "   " })).StatusCode);

        // Someone else's message cannot be edited or deleted.
        var other = ClientFor(_freelancerChat);
        Assert.Equal(HttpStatusCode.Forbidden, (await other.PutAsJsonAsync($"{MessagesUrl}/{id}", new { body = "mine now" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await other.DeleteAsync($"{MessagesUrl}/{id}")).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await me.DeleteAsync($"{MessagesUrl}/{id}")).StatusCode);
        var gone = await deleted.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal(id, gone.GetProperty("id").GetInt32());
        Assert.Equal(_project.Id, gone.GetProperty("project_id").GetInt32());
    }

    [Fact]
    public async Task Unread_badge_counts_then_clears_after_reading_over_REST()
    {
        await ClientFor(_member).PostAsJsonAsync(MessagesUrl, new { body = "one" });
        var second = await (await ClientFor(_member).PostAsJsonAsync(MessagesUrl, new { body = "two" }))
            .Content.ReadFromJsonAsync<JsonElement>();
        var lastId = second.GetProperty("data").GetProperty("id").GetInt32();

        var freelancer = ClientFor(_freelancerChat);
        var unread = await freelancer.GetFromJsonAsync<JsonElement>("/api/v1/chat/unread");
        Assert.Equal(2, unread.GetProperty("data")[0].GetProperty("count").GetInt32());

        var read = await freelancer.PostAsJsonAsync($"/api/v1/projects/{_project.Id}/chat/read", new { last_message_id = lastId });
        Assert.Equal(HttpStatusCode.NoContent, read.StatusCode);

        unread = await freelancer.GetFromJsonAsync<JsonElement>("/api/v1/chat/unread");
        Assert.Equal(0, unread.GetProperty("data").GetArrayLength());
    }

    // ---- WebSocket + ticket: what a browser actually does ----
    // WebSockets cannot carry an Authorization header, so the credential travels in the URL. URLs get
    // logged, so only a one-minute "chat ticket" may go there - never a real access token.

    private async Task<string> TicketFor(User u)
    {
        var res = await ClientFor(u).PostAsync("/api/v1/chat/ticket", null);
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data").GetProperty("ticket").GetString()!;
    }

    private HubConnection WebSocketHub(Func<Task<string>> negotiateToken, Func<Task<string>> queryToken, List<string>? urlsSeen = null)
    {
        return new HubConnectionBuilder()
            .WithUrl(new Uri(_factory.Server.BaseAddress, "/hubs/chat"), o =>
            {
                o.Transports = HttpTransportType.WebSockets;
                o.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
                o.AccessTokenProvider = async () => await negotiateToken();
                o.WebSocketFactory = async (ctx, ct) =>
                {
                    // Do what the browser client does: token in the URL, no Authorization header
                    // (the .NET client would otherwise send a header and hide a broken query path).
                    var uri = new UriBuilder(ctx.Uri)
                    {
                        Query = $"{ctx.Uri.Query.TrimStart('?')}&access_token={Uri.EscapeDataString(await queryToken())}".TrimStart('&'),
                    }.Uri;
                    urlsSeen?.Add(uri.ToString());
                    return await _factory.Server.CreateWebSocketClient().ConnectAsync(uri, ct);
                };
            })
            .Build();
    }

    [Fact]
    public async Task Over_WebSockets_a_chat_ticket_in_the_query_string_authenticates_the_hub()
    {
        var urlsSeen = new List<string>();
        var received = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var hub = WebSocketHub(() => TicketFor(_member), () => TicketFor(_member), urlsSeen);
        hub.On<JsonElement>("MessageReceived", m => received.TrySetResult(m));

        await hub.StartAsync();
        await hub.InvokeAsync("JoinProject", _project.Id);
        Assert.Contains(urlsSeen, u => u.Contains("access_token="));     // really used the query string

        await ClientFor(_freelancerChat).PostAsJsonAsync(MessagesUrl, new { body = "over a websocket" });
        Assert.Equal("over a websocket", (await received.Task.WaitAsync(TimeSpan.FromSeconds(10))).GetProperty("body").GetString());
    }

    [Fact]
    public async Task A_full_access_token_in_the_query_string_is_refused_even_on_the_hub()
    {
        // Negotiation succeeds (ticket in the header); the WebSocket handshake carries a REAL access token in the URL.
        await using var hub = WebSocketHub(() => TicketFor(_member), () => Task.FromResult(TokenFor(_member)));
        await Assert.ThrowsAnyAsync<Exception>(() => hub.StartAsync());
    }

    [Fact]
    public async Task A_chat_ticket_is_useless_anywhere_but_the_hub()
    {
        var ticket = await TicketFor(_member);

        var asBearer = _factory.CreateClient();
        asBearer.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ticket);
        Assert.Equal(HttpStatusCode.Unauthorized, (await asBearer.GetAsync("/api/v1/chat/unread")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await asBearer.GetAsync(MessagesUrl)).StatusCode);

        Assert.Equal(HttpStatusCode.Unauthorized,
            (await _factory.CreateClient().GetAsync($"/api/v1/chat/unread?access_token={ticket}")).StatusCode);
    }

    [Fact]
    public async Task A_refresh_token_cannot_be_used_as_an_access_token()
    {
        string refresh;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            refresh = scope.ServiceProvider.GetRequiredService<ITokenService>()
                .GenerateRefreshToken(db.Users.First(u => u.Id == _member.Id));
        }
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", refresh);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/auth/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(MessagesUrl)).StatusCode);
    }

    [Fact]
    public async Task A_ticket_lives_about_a_minute_and_is_only_issued_to_people_who_may_chat()
    {
        var ticket = await TicketFor(_member);
        var jwt = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().ReadJwtToken(ticket);
        var lifetime = jwt.ValidTo - DateTime.UtcNow;
        Assert.InRange(lifetime.TotalSeconds, 1, 61);
        Assert.Equal("chat-ticket", jwt.Claims.First(c => c.Type == "type").Value);

        Assert.Equal(HttpStatusCode.Forbidden, (await ClientFor(_freelancerNoChat).PostAsync("/api/v1/chat/ticket", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _factory.CreateClient().PostAsync("/api/v1/chat/ticket", null)).StatusCode);
    }

    // ---- live revocation ----

    private async Task<(HubConnection Hub, TaskCompletionSource<JsonElement> Revoked, TaskCompletionSource<JsonElement> Received)>
        ConnectedFreelancer()
    {
        var revoked = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
        var received = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
        var hub = HubFor(_freelancerChat);
        hub.On<JsonElement>("AccessRevoked", m => revoked.TrySetResult(m));
        hub.On<JsonElement>("MessageReceived", m => received.TrySetResult(m));
        await hub.StartAsync();
        await hub.InvokeAsync("JoinProject", _project.Id);
        return (hub, revoked, received);
    }

    [Fact]
    public async Task Removing_someone_from_the_project_cuts_off_their_open_chat_immediately()
    {
        var (hub, revoked, received) = await ConnectedFreelancer();
        await using var _ = hub;

        var removed = await ClientFor(_member).DeleteAsync($"/api/v1/projects/{_project.Id}/members/{_freelancerChat.Id}");
        Assert.True(removed.IsSuccessStatusCode, removed.StatusCode.ToString());

        var told = await revoked.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal(_project.Id, told.GetProperty("project_id").GetInt32());

        await ClientFor(_member).PostAsJsonAsync(MessagesUrl, new { body = "you should not see this" });
        var heard = await Task.WhenAny(received.Task, Task.Delay(1500));
        Assert.NotSame(received.Task, heard);                                  // nothing was pushed to them
        Assert.Equal(HttpStatusCode.Forbidden, (await ClientFor(_freelancerChat).GetAsync(MessagesUrl)).StatusCode);
    }

    [Fact]
    public async Task Taking_chat_permission_away_from_a_role_cuts_off_open_chats()
    {
        var (hub, revoked, received) = await ConnectedFreelancer();
        await using var _ = hub;

        var roleId = (await ReadRoleId(_freelancerChat))!.Value;
        var change = await ClientFor(_member).PostAsJsonAsync("/api/v1/masters/roles/bulk-permissions",
            new { role_permissions = new Dictionary<int, object> { [roleId] = new { proj_view = "A" } } });
        Assert.True(change.IsSuccessStatusCode, change.StatusCode.ToString());

        await revoked.Task.WaitAsync(TimeSpan.FromSeconds(10));
        await ClientFor(_member).PostAsJsonAsync(MessagesUrl, new { body = "after the change" });
        Assert.NotSame(received.Task, await Task.WhenAny(received.Task, Task.Delay(1500)));
    }

    private async Task<int?> ReadRoleId(User u)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return (await db.Users.FirstAsync(x => x.Id == u.Id)).RoleId;
    }

    [Fact]
    public async Task Someone_who_still_has_access_is_not_disturbed_by_another_persons_removal()
    {
        var staysRevoked = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
        var received = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var memberHub = HubFor(_member);
        memberHub.On<JsonElement>("AccessRevoked", m => staysRevoked.TrySetResult(m));
        memberHub.On<JsonElement>("MessageReceived", m => received.TrySetResult(m));
        await memberHub.StartAsync();
        await memberHub.InvokeAsync("JoinProject", _project.Id);

        await ClientFor(_member).DeleteAsync($"/api/v1/projects/{_project.Id}/members/{_freelancerChat.Id}");
        await ClientFor(_freelancerNoChat).PostAsJsonAsync(MessagesUrl, new { body = "x" });   // refused, but harmless
        await ClientFor(_member).PostAsJsonAsync(MessagesUrl, new { body = "still here" });

        Assert.Equal("still here", (await received.Task.WaitAsync(TimeSpan.FromSeconds(10))).GetProperty("body").GetString());
        Assert.False(staysRevoked.Task.IsCompleted);
    }

    // ---- attachments over HTTP ----

    private static readonly byte[] PngBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3, 4 };

    private static MultipartFormDataContent Form(string body, params (string Name, byte[] Bytes)[] files)
    {
        var form = new MultipartFormDataContent { { new StringContent(body), "body" } };
        foreach (var (name, bytes) in files)
        {
            var part = new ByteArrayContent(bytes);
            part.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            form.Add(part, "files", name);
        }
        return form;
    }

    [Fact]
    public async Task A_file_is_pushed_live_and_downloaded_safely_by_room_members_only()
    {
        var received = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var hub = HubFor(_member);
        hub.On<JsonElement>("MessageReceived", m => received.TrySetResult(m));
        await hub.StartAsync();
        await hub.InvokeAsync("JoinProject", _project.Id);

        var sent = await ClientFor(_freelancerChat).PostAsync(MessagesUrl, Form("see attached", ("pic.png", PngBytes)));
        Assert.Equal(HttpStatusCode.Created, sent.StatusCode);

        var pushed = await received.Task.WaitAsync(TimeSpan.FromSeconds(10));
        var attachment = pushed.GetProperty("attachments")[0];
        Assert.Equal("pic.png", attachment.GetProperty("file_name").GetString());
        Assert.Equal("image/png", attachment.GetProperty("content_type").GetString());
        var url = $"/api/v1/projects/{_project.Id}/chat/attachments/{attachment.GetProperty("id").GetInt32()}";

        var download = await ClientFor(_member).GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, download.StatusCode);
        Assert.Equal(PngBytes, await download.Content.ReadAsByteArrayAsync());
        Assert.Equal("nosniff", download.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("attachment", download.Content.Headers.ContentDisposition!.DispositionType);   // never rendered as a page
        Assert.Contains("no-store", download.Headers.CacheControl!.ToString());

        Assert.Equal(HttpStatusCode.Forbidden, (await ClientFor(_stranger).GetAsync(url)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await ClientFor(_freelancerNoChat).GetAsync(url)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _factory.CreateClient().GetAsync(url)).StatusCode);
    }

    [Fact]
    public async Task Bad_uploads_are_rejected_with_a_clear_400_and_nothing_is_stored()
    {
        var client = ClientFor(_member);

        var exe = await client.PostAsync(MessagesUrl, Form("x", ("run.exe", new byte[] { 0x4D, 0x5A, 0, 0 })));
        Assert.Equal(HttpStatusCode.BadRequest, exe.StatusCode);
        Assert.Contains("not an allowed file type", await exe.Content.ReadAsStringAsync());

        var fake = await client.PostAsync(MessagesUrl, Form("x", ("fake.png", "not really a png"u8.ToArray())));
        Assert.Equal(HttpStatusCode.BadRequest, fake.StatusCode);

        var huge = await client.PostAsync(MessagesUrl, Form("x", ("big.png", new byte[10 * 1024 * 1024 + 1])));
        Assert.Equal(HttpStatusCode.BadRequest, huge.StatusCode);

        var history = await client.GetFromJsonAsync<JsonElement>(MessagesUrl);
        Assert.Equal(0, history.GetProperty("data").GetArrayLength());
    }

    [Fact]
    public async Task A_read_only_external_member_cannot_upload()
    {
        // _freelancerNoChat has no chat permissions at all; the endpoint map refuses before any file is read.
        var res = await ClientFor(_freelancerNoChat).PostAsync(MessagesUrl, Form("x", ("a.png", PngBytes)));
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task A_token_in_the_query_string_is_NOT_accepted_on_ordinary_API_endpoints()
    {
        var url = $"/api/v1/chat/unread?access_token={TokenFor(_member)}";
        Assert.Equal(HttpStatusCode.Unauthorized, (await _factory.CreateClient().GetAsync(url)).StatusCode);
    }
}
