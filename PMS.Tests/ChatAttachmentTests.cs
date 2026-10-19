using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using PMS.Application.Common;
using PMS.Application.DTOs;
using PMS.Application.Services.Implementations;
using PMS.Domain.Constants;
using PMS.Domain.Entities;
using PMS.Infrastructure.Data;
using Task = System.Threading.Tasks.Task;

namespace PMS.Tests;

public class ChatAttachmentTests
{
    private const string ChatPerms = """{"chat-view":true,"chat-post":true}""";

    private static ChatService As(ApplicationDbContext db) =>
        new(NullLogger<ChatService>.Instance, db, new AccessScope(db));

    // The first bytes real files start with.
    private static byte[] Png(int extra = 16) => new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }.Concat(new byte[extra]).ToArray();
    private static byte[] Pdf() => "%PDF-1.7\n%test"u8.ToArray();
    private static byte[] Zip() => new byte[] { 0x50, 0x4B, 0x03, 0x04, 0, 0, 0, 0 };
    private static ChatUpload Up(string name, byte[] content, string type = "application/octet-stream") => new(name, type, content);

    private sealed class Room : IDisposable
    {
        public required ApplicationDbContext Db { get; init; }
        public required User Member { get; init; }
        public required User Stranger { get; init; }
        public required User Freelancer { get; init; }
        public required Project Project { get; init; }
        public required Project OtherProject { get; init; }
        public void Dispose() => Db.Dispose();
    }

    private static async Task<Room> BuildAsync()
    {
        var db = TestSupport.NewContext();
        var (admin, employee, freelancer, active, _) = await TestSupport.SeedRolesAsync(db, ChatPerms);
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
        other.TeamMembers.Add(new ProjectMember { UserId = m.Id });
        db.Projects.AddRange(p, other);
        await db.SaveChangesAsync();
        return new Room { Db = db, Member = m, Stranger = s, Freelancer = f, Project = p, OtherProject = other };
    }

    // ---- happy path ----

    [Fact]
    public async Task A_message_can_carry_files_and_lists_them_without_their_bytes()
    {
        using var r = await BuildAsync();
        var msg = await As(r.Db).PostAsync(r.Project.Id, r.Freelancer.Id, "see attached",
            [Up("design.png", Png(), "text/html"), Up("spec.pdf", Pdf())]);

        Assert.Equal(2, msg.Attachments.Count);
        Assert.Equal("image/png", msg.Attachments[0].ContentType);        // from the validated extension, not the client's claim
        Assert.Equal("application/pdf", msg.Attachments[1].ContentType);

        var listed = (await As(r.Db).GetMessagesAsync(r.Project.Id, r.Member.Id, null, 50)).Single();
        Assert.Equal(new[] { "design.png", "spec.pdf" }, listed.Attachments.Select(a => a.FileName));
        Assert.Equal(Png().Length, listed.Attachments[0].Size);
    }

    [Fact]
    public async Task A_file_only_message_is_allowed_but_a_totally_empty_one_is_not()
    {
        using var r = await BuildAsync();
        var msg = await As(r.Db).PostAsync(r.Project.Id, r.Member.Id, "", [Up("a.pdf", Pdf())]);
        Assert.Equal("", msg.Body);

        await Assert.ThrowsAsync<InvalidOperationException>(() => As(r.Db).PostAsync(r.Project.Id, r.Member.Id, "  ", []));
    }

    [Fact]
    public async Task Room_members_can_download_with_the_original_bytes()
    {
        using var r = await BuildAsync();
        var msg = await As(r.Db).PostAsync(r.Project.Id, r.Member.Id, "x", [Up("a.pdf", Pdf())]);

        var file = await As(r.Db).GetAttachmentAsync(r.Project.Id, msg.Attachments[0].Id, r.Freelancer.Id);
        Assert.NotNull(file);
        Assert.Equal(Pdf(), file!.Content);
        Assert.Equal("a.pdf", file.FileName);
    }

    // ---- who may fetch ----

    [Fact]
    public async Task Strangers_cannot_download_and_an_id_from_another_room_finds_nothing()
    {
        using var r = await BuildAsync();
        var mine = await As(r.Db).PostAsync(r.Project.Id, r.Member.Id, "x", [Up("a.pdf", Pdf())]);
        var elsewhere = await As(r.Db).PostAsync(r.OtherProject.Id, r.Member.Id, "y", [Up("b.pdf", Pdf())]);

        await Assert.ThrowsAsync<AccessDeniedException>(
            () => As(r.Db).GetAttachmentAsync(r.Project.Id, mine.Attachments[0].Id, r.Stranger.Id));

        // The freelancer is in room one only: asking room one for room two's file must not leak it.
        Assert.Null(await As(r.Db).GetAttachmentAsync(r.Project.Id, elsewhere.Attachments[0].Id, r.Freelancer.Id));
    }

    [Fact]
    public async Task Deleting_the_message_makes_its_files_unreachable()
    {
        using var r = await BuildAsync();
        var msg = await As(r.Db).PostAsync(r.Project.Id, r.Member.Id, "x", [Up("a.pdf", Pdf())]);
        await As(r.Db).DeleteAsync(r.Project.Id, msg.Id, r.Member.Id);

        Assert.Null(await As(r.Db).GetAttachmentAsync(r.Project.Id, msg.Attachments[0].Id, r.Member.Id));
    }

    // ---- what is refused ----

    [Theory]
    [InlineData("virus.exe")]
    [InlineData("page.html")]
    [InlineData("image.svg")]        // can carry script
    [InlineData("script.js")]
    [InlineData("noextension")]
    [InlineData("archive.tar.gz")]
    public async Task Disallowed_file_types_are_refused(string name)
    {
        using var r = await BuildAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => As(r.Db).PostAsync(r.Project.Id, r.Member.Id, "x", [Up(name, Png())]));
        Assert.Empty(r.Db.ChatMessages);       // nothing half-saved
        Assert.Empty(r.Db.ChatAttachments);
    }

    [Theory]
    [InlineData("fake.png")]         // text pretending to be an image
    [InlineData("fake.pdf")]
    [InlineData("fake.docx")]
    [InlineData("fake.zip")]
    public async Task A_file_whose_bytes_do_not_match_its_extension_is_refused(string name)
    {
        using var r = await BuildAsync();
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => As(r.Db).PostAsync(r.Project.Id, r.Member.Id, "x", [Up(name, "MZ this is really an executable"u8.ToArray())]));
        Assert.Contains("does not look like", ex.Message);
    }

    [Fact]
    public async Task Binary_content_is_refused_for_plain_text_extensions()
    {
        using var r = await BuildAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => As(r.Db).PostAsync(r.Project.Id, r.Member.Id, "x", [Up("notes.txt", new byte[] { 65, 0, 66 })]));
        await As(r.Db).PostAsync(r.Project.Id, r.Member.Id, "x", [Up("notes.txt", "hello"u8.ToArray())]);   // real text is fine
    }

    [Fact]
    public async Task Size_and_count_limits_are_enforced_at_the_boundary()
    {
        using var r = await BuildAsync();
        var atLimit = Png(AppConstants.ChatMaxAttachmentBytes - 8);                 // exactly the maximum
        await As(r.Db).PostAsync(r.Project.Id, r.Member.Id, "big but ok", [Up("max.png", atLimit)]);

        var over = Png(AppConstants.ChatMaxAttachmentBytes - 8 + 1);                // one byte too many
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => As(r.Db).PostAsync(r.Project.Id, r.Member.Id, "x", [Up("over.png", over)]));

        var many = Enumerable.Range(0, AppConstants.ChatMaxAttachments + 1).Select(i => Up($"f{i}.pdf", Pdf())).ToList();
        await Assert.ThrowsAsync<InvalidOperationException>(() => As(r.Db).PostAsync(r.Project.Id, r.Member.Id, "x", many));
        var five = many.Take(AppConstants.ChatMaxAttachments).ToList();
        await As(r.Db).PostAsync(r.Project.Id, r.Member.Id, "x", five);
    }

    [Fact]
    public async Task Empty_files_are_refused()
    {
        using var r = await BuildAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => As(r.Db).PostAsync(r.Project.Id, r.Member.Id, "x", [Up("a.png", [])]));
    }

    [Theory]
    [InlineData("..\\..\\windows\\evil.png", "evil.png")]
    [InlineData("../../etc/passwd.png", "passwd.png")]
    [InlineData("C:\\Users\\me\\photo.png", "photo.png")]
    [InlineData("na\"me.png", "name.png")]
    public async Task File_names_are_reduced_to_a_safe_leaf_name(string given, string expected)
    {
        using var r = await BuildAsync();
        var msg = await As(r.Db).PostAsync(r.Project.Id, r.Member.Id, "x", [Up(given, Png())]);
        Assert.Equal(expected, msg.Attachments.Single().FileName);
    }

    [Fact]
    public async Task A_read_only_external_user_cannot_attach()
    {
        using var r = await BuildAsync();
        var role = await r.Db.Roles.FirstAsync(x => x.Name == AppConstants.RoleFreelancer);
        role.Permissions = """{"chat-view":true}""";
        await r.Db.SaveChangesAsync();

        await Assert.ThrowsAsync<AccessDeniedException>(
            () => As(r.Db).PostAsync(r.Project.Id, r.Freelancer.Id, "x", [Up("a.pdf", Pdf())]));
    }

    // ---- interplay with editing / mentions ----

    [Fact]
    public async Task A_message_with_files_can_have_its_text_cleared_but_a_plain_one_cannot()
    {
        using var r = await BuildAsync();
        var withFile = await As(r.Db).PostAsync(r.Project.Id, r.Member.Id, "caption", [Up("a.pdf", Pdf())]);
        var edited = await As(r.Db).EditAsync(r.Project.Id, withFile.Id, r.Member.Id, "");
        Assert.Equal("", edited!.Body);
        Assert.Single(edited.Attachments);

        var plain = await As(r.Db).PostAsync(r.Project.Id, r.Member.Id, "text only");
        await Assert.ThrowsAsync<InvalidOperationException>(() => As(r.Db).EditAsync(r.Project.Id, plain.Id, r.Member.Id, ""));
    }

    [Fact]
    public async Task Validated_mentions_count_and_forged_ones_do_not()
    {
        using var r = await BuildAsync();
        // Real mention of the freelancer, a forged token for a stranger, and a hand-typed bare id.
        await As(r.Db).PostAsync(r.Project.Id, r.Member.Id,
            $"@[x]({r.Freelancer.Id}) @[y]({r.Stranger.Id}) ]({r.Freelancer.Id})");

        var listed = (await As(r.Db).GetMessagesAsync(r.Project.Id, r.Member.Id, null, 50)).Single();
        Assert.Contains($"]({r.Freelancer.Id})", listed.Body);
        Assert.DoesNotContain($"]({r.Stranger.Id})", listed.Body.Replace($"({r.Freelancer.Id})", ""));   // demoted to plain text
        Assert.Equal(1, (await As(r.Db).GetUnreadAsync(r.Freelancer.Id)).Single().MentionCount);
        Assert.Empty(await r.Db.ChatMentions.Where(m => m.UserId == r.Stranger.Id).ToListAsync());
    }

    [Fact]
    public async Task Editing_a_message_updates_who_is_mentioned()
    {
        using var r = await BuildAsync();
        var msg = await As(r.Db).PostAsync(r.Project.Id, r.Member.Id, $"@[x]({r.Freelancer.Id}) hi");
        Assert.Equal(1, (await As(r.Db).GetUnreadAsync(r.Freelancer.Id)).Single().MentionCount);

        await As(r.Db).EditAsync(r.Project.Id, msg.Id, r.Member.Id, "never mind");
        Assert.Equal(0, (await As(r.Db).GetUnreadAsync(r.Freelancer.Id)).Single().MentionCount);
    }
}
