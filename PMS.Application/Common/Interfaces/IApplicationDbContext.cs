using Microsoft.EntityFrameworkCore;
using PMS.Domain.Entities;

namespace PMS.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<Role> Roles { get; }
    DbSet<Team> Teams { get; }
    DbSet<Skill> Skills { get; }
    DbSet<UserStatus> UserStatuses { get; }
    DbSet<MasterLookup> MasterLookups { get; }
    DbSet<ProjectGroup> ProjectGroups { get; }
    DbSet<ProjectMember> ProjectMembers { get; }
    DbSet<Project> Projects { get; }
    DbSet<Milestone> Milestones { get; }
    DbSet<TaskList> TaskLists { get; }
    DbSet<PMS.Domain.Entities.Task> Tasks { get; }
    DbSet<Issue> Issues { get; }
    DbSet<TimeLog> Timelogs { get; }
    DbSet<Timesheet> Timesheets { get; }
    DbSet<Document> Documents { get; }
    DbSet<DocumentRoleAccess> DocumentRoleAccesses { get; }
    DbSet<DocumentUserAccess> DocumentUserAccesses { get; }
    DbSet<ProjectCredential> ProjectCredentials { get; }
    DbSet<ProjectCredentialRoleAccess> ProjectCredentialRoleAccesses { get; }
    DbSet<ProjectCredentialUserAccess> ProjectCredentialUserAccesses { get; }
    DbSet<ProjectCredentialAccessLog> ProjectCredentialAccessLogs { get; }
    DbSet<ProjectLink> ProjectLinks { get; }
    DbSet<ProjectLinkRoleAccess> ProjectLinkRoleAccesses { get; }
    DbSet<ProjectLinkUserAccess> ProjectLinkUserAccesses { get; }
    DbSet<ProjectNote> ProjectNotes { get; }
    DbSet<ProjectNoteRoleAccess> ProjectNoteRoleAccesses { get; }
    DbSet<ProjectNoteUserAccess> ProjectNoteUserAccesses { get; }
    DbSet<ChatMessage> ChatMessages { get; }
    DbSet<ChatReadState> ChatReadStates { get; }
    DbSet<ChatMention> ChatMentions { get; }
    DbSet<ChatAttachment> ChatAttachments { get; }
    DbSet<ProjectTemplate> ProjectTemplates { get; }
    DbSet<TemplateTask> TemplateTasks { get; }
    DbSet<AuditFieldsMapping> AuditFieldsMappings { get; }
    DbSet<AuditLogs> AuditLogs { get; }
    DbSet<AuditLogDetails> AuditLogDetails { get; }
    DbSet<AuditMetaDataInfo> AuditMetaDataInfos { get; }

    System.Threading.Tasks.Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
