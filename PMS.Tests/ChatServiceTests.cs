using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using PMS.Application.Common;
using PMS.Application.Services.Implementations;
using PMS.Domain.Constants;
using PMS.Domain.Entities;
using PMS.Infrastructure.Data;
using Task = System.Threading.Tasks.Task;

namespace PMS.Tests;

public class ChatServiceTests
{
    private const string ChatPerms = """{"chat-view":true,"chat-post":true}""";

    // A fresh service (with its own access scope) per caller, like one request per user.
    private static ChatService As(ApplicationDbContext db) =>
        new(NullLogger<ChatService>.Instance, db, new AccessScope(db));

    private sealed class Room : IDisposable
    {
        public required ApplicationDbContext Db { get; init; }
        public required User Admin { get; init; }
        public required User Member { get; init; }      // internal member of the project
        public required User Stranger { get; init; }    // internal, not on the project
        public required User Freelancer { get; init; }  // external member of the project
        public required Project Project { get; init; }
        public required Project OtherProject { get; init; }
        public void Dispose() => Db.Dispose();
    }

    private static async Task<Room> BuildAsync(string freelancerPermissions = ChatPerms)
    {
        var db = TestSupport.NewContext();
        var (admin, employee, freelancer, active, _) = await TestSupport.SeedRolesAsync(db, freelancerPermissions);
        var a = TestSupport.NewUser("admin@x.com", admin.Id, active.Id);
        var m = TestSupport.NewUser("member@x.com", employee.Id, active.Id);
        var s = TestSupport.NewUser("stranger@x.com", employee.Id, active.Id);
        var f = TestSupport.NewUser("free@x.com", freelancer.Id, active.Id, external: true);
        db.Users.AddRange(a, m, s, f);
        await db.SaveChangesAsync();

        var p = TestSupport.NewProject("Room", ownerId: a.Id);
        p.TeamMembers.Add(new ProjectMember { UserId = m.Id });
        p.TeamMembers.Add(new ProjectMember { UserId = f.Id });
        var other = TestSupport.NewProject("Elsewhere", ownerId: a.Id);
        db.Projects.AddRange(p, other);
        await db.SaveChangesAsync();

        return new Room { Db = db, Admin = a, Member = m, Stranger = s, Freelancer = f, Project = p, OtherProject = other };
    }

    // ---- who may enter a room ----

    [Fact]
    public async Task Only_people_tied_to_the_project_and_admins_can_enter_the_room()
    {
        using var r = await BuildAsync();
        Assert.True(await As(r.Db).CanAccessAsync(r.Project.Id, r.Member.Id));
        Assert.True(await As(r.Db).CanAccessAsync(r.Project.Id, r.Freelancer.Id));
        Assert.True(await As(r.Db).CanAccessAsync(r.Project.Id, r.Admin.Id));
        Assert.False(await As(r.Db).CanAccessAsync(r.Project.Id, r.Stranger.Id));
        Assert.False(await As(r.Db).CanAccessAsync(r.OtherProject.Id, r.Member.Id));
        Assert.False(await As(r.Db).CanAccessAsync(9999, r.Admin.Id));   // no such project
    }

    [Fact]
    public async Task A_stranger_can_neither_read_nor_post()
    {
        using var r = await BuildAsync();
        await As(r.Db).PostAsync(r.Project.Id, r.Member.Id, "secret plans");

        await Assert.ThrowsAsync<AccessDeniedException>(() => As(r.Db).GetMessagesAsync(r.Project.Id, r.Stranger.Id, null, 50));
        await Assert.ThrowsAsync<AccessDeniedException>(() => As(r.Db).PostAsync(r.Project.Id, r.Stranger.Id, "hi"));
        await Assert.ThrowsAsync<AccessDeniedException>(() => As(r.Db).MarkReadAsync(r.Project.Id, r.Stranger.Id, 1));
    }

    [Fact]
    public async Task Removing_someone_from_the_project_closes_the_room_to_them()
    {
        using var r = await BuildAsync();
        Assert.True(await As(r.Db).CanAccessAsync(r.Project.Id, r.Freelancer.Id));

        r.Db.ProjectMembers.Remove(await r.Db.ProjectMembers.SingleAsync(m => m.UserId == r.Freelancer.Id));
        await r.Db.SaveChangesAsync();

        Assert.False(await As(r.Db).CanAccessAsync(r.Project.Id, r.Freelancer.Id));
    }

    // ---- external users need the chat permissions ----

    [Fact]
    public async Task External_user_with_no_chat_permission_is_locked_out_even_as_a_member()
    {
        using var r = await BuildAsync("""{"proj-view":"A"}""");
        Assert.False(await As(r.Db).CanAccessAsync(r.Project.Id, r.Freelancer.Id));
        await Assert.ThrowsAsync<AccessDeniedException>(() => As(r.Db).GetMessagesAsync(r.Project.Id, r.Freelancer.Id, null, 50));
        Assert.Empty(await As(r.Db).GetUnreadAsync(r.Freelancer.Id));
    }

    [Fact]
    public async Task External_user_with_chat_view_only_can_read_but_not_write()
    {
        using var r = await BuildAsync("""{"chat-view":true}""");
        await As(r.Db).PostAsync(r.Project.Id, r.Member.Id, "hello team");

        var seen = await As(r.Db).GetMessagesAsync(r.Project.Id, r.Freelancer.Id, null, 50);
        Assert.Single(seen);
        await Assert.ThrowsAsync<AccessDeniedException>(() => As(r.Db).PostAsync(r.Project.Id, r.Freelancer.Id, "hi"));
    }

    // ---- posting rules ----

    [Fact]
    public async Task Posting_stores_the_message_with_the_sender_and_returns_it_oldest_first()
    {
        using var r = await BuildAsync();
        var first = await As(r.Db).PostAsync(r.Project.Id, r.Member.Id, "  first  ");
        await As(r.Db).PostAsync(r.Project.Id, r.Freelancer.Id, "second");

        Assert.Equal("first", first.Body);                       // trimmed
        var all = await As(r.Db).GetMessagesAsync(r.Project.Id, r.Admin.Id, null, 50);
        Assert.Equal(new[] { "first", "second" }, all.Select(m => m.Body));
        Assert.True(all[1].SenderIsExternal);
        Assert.False(all[0].SenderIsExternal);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Empty_messages_are_rejected(string body)
    {
        using var r = await BuildAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() => As(r.Db).PostAsync(r.Project.Id, r.Member.Id, body));
        Assert.Empty(r.Db.ChatMessages);
    }

    [Fact]
    public async Task Over_long_messages_are_rejected_but_the_limit_itself_is_fine()
    {
        using var r = await BuildAsync();
        await As(r.Db).PostAsync(r.Project.Id, r.Member.Id, new string('x', AppConstants.ChatMaxBodyLength));
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => As(r.Db).PostAsync(r.Project.Id, r.Member.Id, new string('x', AppConstants.ChatMaxBodyLength + 1)));
        Assert.Single(r.Db.ChatMessages);
    }

    [Fact]
    public async Task Messages_page_backwards_with_beforeId()
    {
        using var r = await BuildAsync();
        for (var i = 1; i <= 5; i++) await As(r.Db).PostAsync(r.Project.Id, r.Member.Id, $"m{i}");

        var newest = await As(r.Db).GetMessagesAsync(r.Project.Id, r.Member.Id, null, 2);
        Assert.Equal(new[] { "m4", "m5" }, newest.Select(m => m.Body));
        var older = await As(r.Db).GetMessagesAsync(r.Project.Id, r.Member.Id, newest[0].Id, 2);
        Assert.Equal(new[] { "m2", "m3" }, older.Select(m => m.Body));
    }

    [Fact]
    public async Task Rooms_do_not_leak_into_each_other()
    {
        using var r = await BuildAsync();
        await As(r.Db).PostAsync(r.Project.Id, r.Member.Id, "in room one");
        await As(r.Db).PostAsync(r.OtherProject.Id, r.Admin.Id, "in room two");

        var one = await As(r.Db).GetMessagesAsync(r.Project.Id, r.Member.Id, null, 50);
        Assert.Equal(new[] { "in room one" }, one.Select(m => m.Body));
    }

    // ---- edit / delete ----

    [Fact]
    public async Task You_can_edit_only_your_own_messages()
    {
        using var r = await BuildAsync();
        var msg = await As(r.Db).PostAsync(r.Project.Id, r.Member.Id, "typo");

        var edited = await As(r.Db).EditAsync(r.Project.Id, msg.Id, r.Member.Id, "fixed");
        Assert.Equal("fixed", edited!.Body);
        Assert.NotNull(edited.EditedAt);

        await Assert.ThrowsAsync<AccessDeniedException>(
            () => As(r.Db).EditAsync(r.Project.Id, msg.Id, r.Freelancer.Id, "hijack"));
        await Assert.ThrowsAsync<AccessDeniedException>(     // even admins do not rewrite what others said
            () => As(r.Db).EditAsync(r.Project.Id, msg.Id, r.Admin.Id, "hijack"));
        Assert.Null(await As(r.Db).EditAsync(r.Project.Id, 9999, r.Member.Id, "x"));
    }

    [Fact]
    public async Task Delete_hides_the_message_keeps_the_row_and_is_limited_to_owner_or_admin()
    {
        using var r = await BuildAsync();
        var mine = await As(r.Db).PostAsync(r.Project.Id, r.Member.Id, "mine");
        var theirs = await As(r.Db).PostAsync(r.Project.Id, r.Freelancer.Id, "theirs");

        await Assert.ThrowsAsync<AccessDeniedException>(
            () => As(r.Db).DeleteAsync(r.Project.Id, theirs.Id, r.Member.Id));    // not yours

        Assert.True(await As(r.Db).DeleteAsync(r.Project.Id, mine.Id, r.Member.Id));       // yours
        Assert.True(await As(r.Db).DeleteAsync(r.Project.Id, theirs.Id, r.Admin.Id));      // admin moderation

        Assert.Empty(await As(r.Db).GetMessagesAsync(r.Project.Id, r.Admin.Id, null, 50));
        Assert.Equal(2, await r.Db.ChatMessages.IgnoreQueryFilters().CountAsync());          // still on record
        Assert.False(await As(r.Db).DeleteAsync(r.Project.Id, mine.Id, r.Member.Id));        // already gone
    }

    [Fact]
    public async Task A_message_id_from_another_room_cannot_be_edited_or_deleted_through_this_one()
    {
        using var r = await BuildAsync();
        var elsewhere = await As(r.Db).PostAsync(r.OtherProject.Id, r.Admin.Id, "other room");

        Assert.Null(await As(r.Db).EditAsync(r.Project.Id, elsewhere.Id, r.Member.Id, "x"));
        Assert.False(await As(r.Db).DeleteAsync(r.Project.Id, elsewhere.Id, r.Member.Id));
    }

    // ---- unread counts ----

    [Fact]
    public async Task Unread_counts_only_other_peoples_messages_and_clear_when_read()
    {
        using var r = await BuildAsync();
        await As(r.Db).PostAsync(r.Project.Id, r.Member.Id, "a");
        await As(r.Db).PostAsync(r.Project.Id, r.Member.Id, "b");
        var last = await As(r.Db).PostAsync(r.Project.Id, r.Freelancer.Id, "c");

        var forFreelancer = await As(r.Db).GetUnreadAsync(r.Freelancer.Id);
        Assert.Equal(2, forFreelancer.Single(u => u.ProjectId == r.Project.Id).Count);   // own "c" not counted

        var forMember = await As(r.Db).GetUnreadAsync(r.Member.Id);
        Assert.Equal(1, forMember.Single().Count);                                        // only "c"

        Assert.Empty(await As(r.Db).GetUnreadAsync(r.Stranger.Id));                       // not their room

        await As(r.Db).MarkReadAsync(r.Project.Id, r.Freelancer.Id, last.Id);
        Assert.Empty(await As(r.Db).GetUnreadAsync(r.Freelancer.Id));
    }

    [Fact]
    public async Task Marking_read_cannot_run_ahead_of_real_messages_or_move_backwards()
    {
        using var r = await BuildAsync();
        var one = await As(r.Db).PostAsync(r.Project.Id, r.Member.Id, "one");
        await As(r.Db).MarkReadAsync(r.Project.Id, r.Freelancer.Id, 1_000_000);   // bogus, far in the future
        await As(r.Db).PostAsync(r.Project.Id, r.Member.Id, "two");

        Assert.Equal(1, (await As(r.Db).GetUnreadAsync(r.Freelancer.Id)).Single().Count);   // "two" still unread

        await As(r.Db).MarkReadAsync(r.Project.Id, r.Freelancer.Id, one.Id);                 // older id: ignored
        Assert.Equal(1, (await As(r.Db).GetUnreadAsync(r.Freelancer.Id)).Single().Count);
    }

    [Fact]
    public async Task Unread_counts_exclude_deleted_messages()
    {
        using var r = await BuildAsync();
        var msg = await As(r.Db).PostAsync(r.Project.Id, r.Member.Id, "oops");
        await As(r.Db).DeleteAsync(r.Project.Id, msg.Id, r.Member.Id);
        Assert.Empty(await As(r.Db).GetUnreadAsync(r.Freelancer.Id));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Unread_query_translates_to_sql_server(bool admin)
    {
        using var sql = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=localhost;Database=never_connected;Trusted_Connection=True;").Options);
        var service = new ChatService(NullLogger<ChatService>.Instance, sql, new AccessScope(sql));

        var text = service.BuildUnreadQuery(7, admin).ToQueryString();

        Assert.Contains("GROUP BY", text);
        Assert.Contains("chat_messages", text);
    }

    // ---- @mentions ----

    private static string Mention(User u) => $"@[{u.FirstName}]({u.Id})";

    [Fact]
    public async Task Mention_members_are_the_people_in_the_room_once_each()
    {
        using var r = await BuildAsync();
        var names = (await As(r.Db).GetMembersAsync(r.Project.Id, r.Freelancer.Id)).Select(m => m.Id).ToList();

        Assert.Equal(3, names.Count);                                    // admin (owner), member, freelancer
        Assert.Equal(names.Distinct().Count(), names.Count);
        Assert.DoesNotContain(r.Stranger.Id, names);
        Assert.True((await As(r.Db).GetMembersAsync(r.Project.Id, r.Member.Id)).Single(m => m.Id == r.Freelancer.Id).IsExternal);
    }

    [Fact]
    public async Task Mention_members_list_is_closed_to_strangers_and_hides_deactivated_people()
    {
        using var r = await BuildAsync();
        await Assert.ThrowsAsync<AccessDeniedException>(() => As(r.Db).GetMembersAsync(r.Project.Id, r.Stranger.Id));

        (await r.Db.Users.FindAsync(r.Member.Id))!.IsActive = false;
        await r.Db.SaveChangesAsync();
        Assert.DoesNotContain(r.Member.Id, (await As(r.Db).GetMembersAsync(r.Project.Id, r.Admin.Id)).Select(m => m.Id));
    }

    [Fact]
    public async Task A_mention_counts_as_unread_for_the_person_mentioned_only()
    {
        using var r = await BuildAsync();
        await As(r.Db).PostAsync(r.Project.Id, r.Member.Id, $"hello {Mention(r.Freelancer)}");
        await As(r.Db).PostAsync(r.Project.Id, r.Member.Id, "no mention here");

        var forFreelancer = (await As(r.Db).GetUnreadAsync(r.Freelancer.Id)).Single();
        Assert.Equal(2, forFreelancer.Count);
        Assert.Equal(1, forFreelancer.MentionCount);

        var forAdmin = (await As(r.Db).GetUnreadAsync(r.Admin.Id)).Single();
        Assert.Equal(2, forAdmin.Count);
        Assert.Equal(0, forAdmin.MentionCount);
    }

    [Fact]
    public async Task Mentioning_yourself_or_a_similar_looking_id_does_not_count()
    {
        using var r = await BuildAsync();
        await As(r.Db).PostAsync(r.Project.Id, r.Freelancer.Id, $"note to self {Mention(r.Freelancer)}");     // own message
        await As(r.Db).PostAsync(r.Project.Id, r.Member.Id, $"@[Someone]({r.Freelancer.Id}0)");               // id with an extra digit

        var unread = (await As(r.Db).GetUnreadAsync(r.Freelancer.Id)).Single();
        Assert.Equal(1, unread.Count);
        Assert.Equal(0, unread.MentionCount);
    }

    [Fact]
    public async Task Reading_the_room_clears_the_mention_count()
    {
        using var r = await BuildAsync();
        var msg = await As(r.Db).PostAsync(r.Project.Id, r.Member.Id, Mention(r.Freelancer));
        await As(r.Db).MarkReadAsync(r.Project.Id, r.Freelancer.Id, msg.Id);
        Assert.Empty(await As(r.Db).GetUnreadAsync(r.Freelancer.Id));
    }
}
