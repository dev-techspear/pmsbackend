using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using PMS.Application.Common;
using PMS.Application.Common.Interfaces;
using PMS.Application.DTOs.Requests;
using PMS.Application.Services.Implementations;
using PMS.Domain.Entities;
using PMS.Infrastructure.Data;
using PMS.Infrastructure.Services;
using Task = System.Threading.Tasks.Task;

namespace PMS.Tests;

/// <summary>
/// Who can see a project document / credential. The population is enumerated (every user in the
/// fixture), never sampled, and compared with exact-set equality.
/// </summary>
public class ProjectSharingTests
{
    private sealed class FakeBlobs : IBlobStorageService
    {
        public readonly Dictionary<string, byte[]> Store = new();
        public Task<string> UploadAsync(string blobPath, byte[] content, string contentType, CancellationToken ct = default)
        {
            Store[blobPath] = content;
            return Task.FromResult(blobPath);
        }
        public string GetReadSasUrl(string blobPath, TimeSpan validFor, string? downloadFileName = null) => "sas://" + blobPath;
    }

    private static ICredentialProtector Protector(string? key = null) =>
        new AesGcmCredentialProtector(new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["CredentialEncryption:Key"] = key ?? Convert.ToBase64String(new byte[32].Select((_, i) => (byte)(i + 1)).ToArray()) }).Build());

    private sealed class World : IDisposable
    {
        public required ApplicationDbContext Db { get; init; }
        public required Role EmployeeRole { get; init; }
        public required Role FreelancerRole { get; init; }
        public required User Admin { get; init; }
        public required User Uploader { get; init; }       // Employee
        public required User SameRole { get; init; }       // Employee
        public required User OtherRole { get; init; }      // Freelancer
        public required User SharedUser { get; init; }     // Freelancer, shared by name
        public required Project Project { get; init; }
        public FakeBlobs Blobs { get; } = new();
        public void Dispose() => Db.Dispose();

        public IEnumerable<(string Name, User User)> Everyone =>
            [("admin", Admin), ("uploader", Uploader), ("sameRole", SameRole), ("otherRole", OtherRole), ("sharedUser", SharedUser)];

        public async Task<AccessScope> ScopeAsync(User u) { var s = new AccessScope(Db); await s.LoadAsync(u.Id); return s; }

        public async Task<DocumentFileService> FilesAsync(User u) =>
            new(NullLogger<DocumentFileService>.Instance, Db, await ScopeAsync(u), Blobs);

        public async Task<CredentialService> CredsAsync(User u, ICredentialProtector? p = null) =>
            new(NullLogger<CredentialService>.Instance, Db, await ScopeAsync(u), p ?? Protector());

        public async Task<HashSet<string>> WhoSeesDocumentAsync(int docId)
        {
            var seen = new HashSet<string>();
            foreach (var (name, user) in Everyone)
                if (await Db.Documents.ScopeDocuments(await ScopeAsync(user)).AnyAsync(d => d.Id == docId)) seen.Add(name);
            return seen;
        }

        public async Task<HashSet<string>> WhoSeesCredentialAsync(int credId)
        {
            var seen = new HashSet<string>();
            foreach (var (name, user) in Everyone)
                if (await Db.ProjectCredentials.ScopeCredentials(await ScopeAsync(user)).AnyAsync(c => c.Id == credId)) seen.Add(name);
            return seen;
        }
    }

    private static async Task<World> BuildAsync()
    {
        var db = TestSupport.NewContext();
        var (admin, employee, freelancer, active, _) = await TestSupport.SeedRolesAsync(db);
        var a = TestSupport.NewUser("admin@x.com", admin.Id, active.Id);
        var up = TestSupport.NewUser("uploader@x.com", employee.Id, active.Id);
        var same = TestSupport.NewUser("same@x.com", employee.Id, active.Id);
        var other = TestSupport.NewUser("other@x.com", freelancer.Id, active.Id);
        var shared = TestSupport.NewUser("shared@x.com", freelancer.Id, active.Id);
        db.Users.AddRange(a, up, same, other, shared);
        var project = TestSupport.NewProject("P");
        db.Projects.Add(project);
        await db.SaveChangesAsync();
        // Freelancers are scoped to projects they belong to; sharing never widens project access.
        foreach (var u in new[] { up, same, other, shared })
            db.ProjectMembers.Add(new ProjectMember { ProjectId = project.Id, UserId = u.Id });
        await db.SaveChangesAsync();
        return new World
        {
            Db = db, EmployeeRole = employee, FreelancerRole = freelancer,
            Admin = a, Uploader = up, SameRole = same, OtherRole = other, SharedUser = shared, Project = project,
        };
    }

    private static UploadDocumentRequest Req(World w, bool restrict, int[]? roles = null, int[]? users = null) =>
        new(w.Project.Id, "Plan", null, restrict, roles ?? [], users ?? []);

    private static DocumentUploadFile Txt(string name = "plan.txt") => new(name, "text/plain", "hello"u8.ToArray());

    // ---------------------------------------------------------------- documents

    [Fact]
    public async Task Unrestricted_document_is_visible_to_everyone_on_the_project()
    {
        using var w = await BuildAsync();
        var doc = await (await w.FilesAsync(w.Uploader)).UploadDocumentAsync(Req(w, restrict: false), Txt());

        Assert.Equal(new HashSet<string> { "admin", "uploader", "sameRole", "otherRole", "sharedUser" },
            await w.WhoSeesDocumentAsync(doc.Id));
    }

    [Fact]
    public async Task Restricted_document_with_no_shares_is_visible_only_to_uploader_and_admin()
    {
        using var w = await BuildAsync();
        var doc = await (await w.FilesAsync(w.Uploader)).UploadDocumentAsync(Req(w, restrict: true), Txt());

        Assert.Equal(new HashSet<string> { "admin", "uploader" }, await w.WhoSeesDocumentAsync(doc.Id));
    }

    [Fact]
    public async Task Sharing_with_a_role_exposes_it_to_that_role_only()
    {
        using var w = await BuildAsync();
        var doc = await (await w.FilesAsync(w.Uploader))
            .UploadDocumentAsync(Req(w, true, roles: [w.FreelancerRole.Id]), Txt());

        Assert.Equal(new HashSet<string> { "admin", "uploader", "otherRole", "sharedUser" }, await w.WhoSeesDocumentAsync(doc.Id));
    }

    [Fact]
    public async Task Sharing_with_a_named_user_exposes_it_to_that_user_only()
    {
        using var w = await BuildAsync();
        var doc = await (await w.FilesAsync(w.Uploader))
            .UploadDocumentAsync(Req(w, true, users: [w.SharedUser.Id]), Txt());

        Assert.Equal(new HashSet<string> { "admin", "uploader", "sharedUser" }, await w.WhoSeesDocumentAsync(doc.Id));
    }

    [Fact]
    public async Task Share_lists_are_ignored_when_access_is_not_restricted()
    {
        using var w = await BuildAsync();
        var doc = await (await w.FilesAsync(w.Uploader))
            .UploadDocumentAsync(Req(w, false, roles: [w.FreelancerRole.Id]), Txt());

        Assert.False(doc.RestrictAccess);
        Assert.Empty(w.Db.DocumentRoleAccesses);
    }

    [Fact]
    public async Task Download_is_denied_to_a_user_the_document_is_not_shared_with()
    {
        using var w = await BuildAsync();
        var doc = await (await w.FilesAsync(w.Uploader)).UploadDocumentAsync(Req(w, true), Txt());

        Assert.Null(await (await w.FilesAsync(w.OtherRole)).GetDownloadAsync(doc.Id));
        Assert.Null(await (await w.FilesAsync(w.SameRole)).GetDownloadAsync(doc.Id));
        Assert.StartsWith("sas://pms/documents/", (await (await w.FilesAsync(w.Uploader)).GetDownloadAsync(doc.Id))!.Url);
    }

    [Fact]
    public async Task Upload_stores_the_blob_under_the_pms_prefix_and_never_exposes_the_blob_path()
    {
        using var w = await BuildAsync();
        var doc = await (await w.FilesAsync(w.Uploader)).UploadDocumentAsync(Req(w, false), Txt());

        var path = Assert.Single(w.Blobs.Store.Keys);
        Assert.StartsWith($"pms/documents/{w.Project.Id}/", path);
        Assert.Equal($"/api/v1/documents/{doc.Id}/download", doc.FileUrl);
        Assert.DoesNotContain(path, doc.FileUrl);
    }

    [Fact]
    public async Task Only_uploader_or_admin_can_change_sharing()
    {
        using var w = await BuildAsync();
        var doc = await (await w.FilesAsync(w.Uploader)).UploadDocumentAsync(Req(w, false), Txt());
        var change = new DocumentSharingRequest(true, [w.FreelancerRole.Id]);

        await Assert.ThrowsAsync<AccessDeniedException>(async () => await (await w.FilesAsync(w.SameRole)).UpdateSharingAsync(doc.Id, change));
        Assert.NotNull(await (await w.FilesAsync(w.Uploader)).UpdateSharingAsync(doc.Id, change));
        Assert.NotNull(await (await w.FilesAsync(w.Admin)).UpdateSharingAsync(doc.Id, new DocumentSharingRequest(false)));
    }

    [Fact]
    public async Task Upload_rejects_disallowed_extension_empty_and_oversized_files()
    {
        using var w = await BuildAsync();
        var svc = await w.FilesAsync(w.Uploader);

        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.UploadDocumentAsync(Req(w, false), new("evil.exe", "application/octet-stream", [1])));
        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.UploadDocumentAsync(Req(w, false), new("a.txt", "text/plain", [])));
        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.UploadDocumentAsync(Req(w, false),
            new("big.txt", "text/plain", new byte[PMS.Domain.Constants.AppConstants.DocumentMaxBytes + 1])));
        Assert.Empty(w.Blobs.Store);
    }

    [Fact]
    public async Task Sharing_with_an_unknown_role_is_rejected()
    {
        using var w = await BuildAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await (await w.FilesAsync(w.Uploader)).UploadDocumentAsync(Req(w, true, roles: [9999]), Txt()));
    }

    // ---------------------------------------------------------------- credentials

    private static CreateCredentialRequest NewCred(bool restrict, int[]? roles = null, int[]? users = null) =>
        new("Prod DB", "Database", "S3cr3t!pw", "db.local", "sa", null, restrict, roles, users);

    [Fact]
    public async Task Credential_secret_is_stored_encrypted_and_never_in_the_response()
    {
        using var w = await BuildAsync();
        var created = await (await w.CredsAsync(w.Admin)).CreateCredentialAsync(w.Project.Id, NewCred(false));

        var row = await w.Db.ProjectCredentials.SingleAsync();
        Assert.DoesNotContain("S3cr3t", row.SecretEncrypted);
        Assert.StartsWith("v1:", row.SecretEncrypted);
        Assert.DoesNotContain("S3cr3t", System.Text.Json.JsonSerializer.Serialize(created));
        var listed = await (await w.CredsAsync(w.Admin)).GetCredentialsAsync(w.Project.Id);
        Assert.DoesNotContain("S3cr3t", System.Text.Json.JsonSerializer.Serialize(listed));
    }

    [Fact]
    public async Task Credential_sharing_enumerates_exactly_the_allowed_users()
    {
        using var w = await BuildAsync();
        var admins = await w.CredsAsync(w.Admin);
        var byRole = await admins.CreateCredentialAsync(w.Project.Id, NewCred(true, roles: [w.FreelancerRole.Id]));
        var byUser = await admins.CreateCredentialAsync(w.Project.Id, NewCred(true, users: [w.SameRole.Id]));
        var open = await admins.CreateCredentialAsync(w.Project.Id, NewCred(false));
        var priv = await admins.CreateCredentialAsync(w.Project.Id, NewCred(true));

        Assert.Equal(new HashSet<string> { "admin", "otherRole", "sharedUser" }, await w.WhoSeesCredentialAsync(byRole.Id));
        Assert.Equal(new HashSet<string> { "admin", "sameRole" }, await w.WhoSeesCredentialAsync(byUser.Id));
        Assert.Equal(new HashSet<string> { "admin", "uploader", "sameRole", "otherRole", "sharedUser" }, await w.WhoSeesCredentialAsync(open.Id));
        Assert.Equal(new HashSet<string> { "admin" }, await w.WhoSeesCredentialAsync(priv.Id));
    }

    [Fact]
    public async Task Reveal_returns_the_secret_only_to_allowed_users_and_writes_an_audit_row()
    {
        using var w = await BuildAsync();
        var cred = await (await w.CredsAsync(w.Admin)).CreateCredentialAsync(w.Project.Id, NewCred(true, users: [w.SharedUser.Id]));

        Assert.Null(await (await w.CredsAsync(w.OtherRole)).RevealSecretAsync(w.Project.Id, cred.Id));
        Assert.Equal(0, await w.Db.ProjectCredentialAccessLogs.CountAsync(l => l.Action == PMS.Domain.Constants.AppConstants.CredentialActionReveal));

        var secret = await (await w.CredsAsync(w.SharedUser)).RevealSecretAsync(w.Project.Id, cred.Id);
        Assert.Equal("S3cr3t!pw", secret!.Secret);
        var log = await w.Db.ProjectCredentialAccessLogs.SingleAsync(l => l.Action == PMS.Domain.Constants.AppConstants.CredentialActionReveal);
        Assert.Equal(w.SharedUser.Id, log.UserId);
    }

    [Fact]
    public async Task Rotating_replaces_the_secret_and_only_creator_or_admin_can_edit()
    {
        using var w = await BuildAsync();
        var cred = await (await w.CredsAsync(w.Admin)).CreateCredentialAsync(w.Project.Id, NewCred(false));

        await Assert.ThrowsAsync<AccessDeniedException>(async () =>
            await (await w.CredsAsync(w.SameRole)).UpdateCredentialAsync(w.Project.Id, cred.Id, new UpdateCredentialRequest(Label: "x")));

        await (await w.CredsAsync(w.Admin)).UpdateCredentialAsync(w.Project.Id, cred.Id, new UpdateCredentialRequest(Secret: "N3w!"));
        Assert.Equal("N3w!", (await (await w.CredsAsync(w.Admin)).RevealSecretAsync(w.Project.Id, cred.Id))!.Secret);
    }

    [Fact]
    public async Task Creating_a_credential_needs_edit_permission_and_a_known_type()
    {
        using var w = await BuildAsync();
        // Plain employee role has no proj-edit.
        await Assert.ThrowsAsync<AccessDeniedException>(async () =>
            await (await w.CredsAsync(w.Uploader)).CreateCredentialAsync(w.Project.Id, NewCred(false)));
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await (await w.CredsAsync(w.Admin)).CreateCredentialAsync(w.Project.Id, NewCred(false) with { CredentialType = "Bogus" }));
    }

    // ---------------------------------------------------------------- encryption

    [Fact]
    public void Protector_round_trips_and_uses_a_fresh_nonce_each_time()
    {
        var p = Protector();
        var a = p.Protect("hunter2");
        var b = p.Protect("hunter2");
        Assert.NotEqual(a, b);
        Assert.Equal("hunter2", p.Unprotect(a));
        Assert.Equal("päss wörd ✓", p.Unprotect(p.Protect("päss wörd ✓")));
    }

    [Fact]
    public void Protector_rejects_tampered_data_and_the_wrong_key()
    {
        var p = Protector();
        var payload = p.Protect("hunter2");
        var raw = Convert.FromBase64String(payload["v1:".Length..]);
        raw[^1] ^= 0xFF;
        Assert.ThrowsAny<System.Security.Cryptography.CryptographicException>(() => p.Unprotect("v1:" + Convert.ToBase64String(raw)));

        var otherKey = Protector(Convert.ToBase64String(new byte[32].Select(_ => (byte)7).ToArray()));
        Assert.ThrowsAny<System.Security.Cryptography.CryptographicException>(() => otherKey.Unprotect(payload));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("CHANGEME")]
    [InlineData("not-base64!!")]
    [InlineData("AAAA")] // valid base64, wrong length
    public void Protector_refuses_to_run_without_a_valid_32_byte_key(string? key)
    {
        var cfg = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["CredentialEncryption:Key"] = key }).Build();
        Assert.Throws<InvalidOperationException>(() => new AesGcmCredentialProtector(cfg).Protect("x"));
    }
}
