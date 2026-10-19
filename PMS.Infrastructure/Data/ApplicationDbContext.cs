using Microsoft.EntityFrameworkCore;
using PMS.Domain.Entities;
using Task = PMS.Domain.Entities.Task;
using PMS.Application.Common.Interfaces;

namespace PMS.Infrastructure.Data;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<Skill> Skills => Set<Skill>();
    public DbSet<UserStatus> UserStatuses => Set<UserStatus>();
    public DbSet<MasterLookup> MasterLookups => Set<MasterLookup>();
    public DbSet<ProjectGroup> ProjectGroups => Set<ProjectGroup>();
    public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Milestone> Milestones => Set<Milestone>();
    public DbSet<TaskList> TaskLists => Set<TaskList>();
    public DbSet<Task> Tasks => Set<Task>();
    public DbSet<Issue> Issues => Set<Issue>();
    public DbSet<TimeLog> Timelogs => Set<TimeLog>();
    public DbSet<Timesheet> Timesheets => Set<Timesheet>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<ChatReadState> ChatReadStates => Set<ChatReadState>();
    public DbSet<ChatMention> ChatMentions => Set<ChatMention>();
    public DbSet<ChatAttachment> ChatAttachments => Set<ChatAttachment>();
    public DbSet<ProjectTemplate> ProjectTemplates => Set<ProjectTemplate>();
    public DbSet<TemplateTask> TemplateTasks => Set<TemplateTask>();
    public DbSet<AuditFieldsMapping> AuditFieldsMappings => Set<AuditFieldsMapping>();
    public DbSet<AuditLogs> AuditLogs => Set<AuditLogs>();
    public DbSet<AuditLogDetails> AuditLogDetails => Set<AuditLogDetails>();
    public DbSet<AuditMetaDataInfo> AuditMetaDataInfos => Set<AuditMetaDataInfo>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply Global Query Filter for Soft Delete (IsDeleted == false)
        modelBuilder.Entity<User>().HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<Role>().HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<Team>().HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<Skill>().HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<UserStatus>().HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<MasterLookup>().HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<ProjectGroup>().HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<Project>().HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<Milestone>().HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<TaskList>().HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<Task>().HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<Issue>().HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<TimeLog>().HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<Timesheet>().HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<Document>().HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<ProjectTemplate>().HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<ChatMessage>().HasQueryFilter(x => !x.IsDeleted);
        // TemplateTask, AuditFieldsMapping, AuditLogs, AuditLogDetails, AuditMetaDataInfo
        // have no IsDeleted column in source — no soft-delete filter for these.

        // --- UNIQUE CONSTRAINTS & INDEXES ---
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email).IsUnique();
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username).IsUnique();
        modelBuilder.Entity<User>()
            .HasIndex(u => u.PublicId).IsUnique();
        modelBuilder.Entity<User>()
            .HasIndex(u => u.EmployeeId).IsUnique();
        modelBuilder.Entity<User>()
            .HasIndex(u => u.O365Id).IsUnique();

        modelBuilder.Entity<Role>()
            .HasIndex(r => r.Name).IsUnique();

        modelBuilder.Entity<Team>()
            .HasIndex(t => t.TeamEmail).IsUnique();
        modelBuilder.Entity<Team>()
            .HasIndex(t => t.PublicId).IsUnique();

        modelBuilder.Entity<Skill>()
            .HasIndex(s => s.Name).IsUnique();

        modelBuilder.Entity<UserStatus>()
            .HasIndex(us => us.Name).IsUnique();

        modelBuilder.Entity<ProjectGroup>()
            .HasIndex(pg => pg.Name).IsUnique();

        modelBuilder.Entity<MasterLookup>()
            .HasIndex(ml => new { ml.Category, ml.Value }).IsUnique();

        modelBuilder.Entity<Project>()
            .HasIndex(p => p.PublicId).IsUnique();
        modelBuilder.Entity<Project>()
            .HasIndex(p => p.ProjectIdSync).IsUnique();
        modelBuilder.Entity<Project>()
            .HasIndex(p => p.ProjectName).IsUnique();

        modelBuilder.Entity<Milestone>()
            .HasIndex(m => m.PublicId).IsUnique();

        modelBuilder.Entity<Task>()
            .HasIndex(t => t.PublicId).IsUnique();

        modelBuilder.Entity<Issue>()
            .HasIndex(i => i.PublicId).IsUnique();

        // --- RELATIONSHIPS & CASCADE ACTIONS ---

        // User Self-referential Manager Relation (Manager Email -> User Email)
        modelBuilder.Entity<User>()
            .HasOne(u => u.Manager)
            .WithMany()
            .HasForeignKey(u => u.ManagerEmail)
            .HasPrincipalKey(u => u.Email)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<User>()
            .HasOne(u => u.Role)
            .WithMany(r => r.Users)
            .HasForeignKey(u => u.RoleId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<User>()
            .HasOne(u => u.Status)
            .WithMany()
            .HasForeignKey(u => u.StatusId)
            .OnDelete(DeleteBehavior.NoAction);

        // Team Relations
        modelBuilder.Entity<Team>()
            .HasOne(t => t.Lead)
            .WithMany(u => u.ManagedTeams)
            .HasForeignKey(t => t.LeadEmail)
            .HasPrincipalKey(u => u.Email)
            .OnDelete(DeleteBehavior.NoAction);

        // Project Relations
        modelBuilder.Entity<Project>()
            .HasOne(p => p.Owner)
            .WithMany()
            .HasForeignKey(p => p.OwnerId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Project>()
            .HasOne(p => p.ProjectManager)
            .WithMany()
            .HasForeignKey(p => p.ProjectManagerId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Project>()
            .HasOne(p => p.DeliveryHead)
            .WithMany()
            .HasForeignKey(p => p.DeliveryHeadId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Project>()
            .HasOne(p => p.StatusMaster)
            .WithMany()
            .HasForeignKey(p => p.StatusId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Project>()
            .HasOne(p => p.PriorityMaster)
            .WithMany()
            .HasForeignKey(p => p.PriorityId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Project>()
            .HasOne(p => p.PreviousStatusMaster)
            .WithMany()
            .HasForeignKey(p => p.PreviousStatusId)
            .OnDelete(DeleteBehavior.NoAction);

        // ProjectMember Composite Key & Relations
        modelBuilder.Entity<ProjectMember>()
            .HasKey(pm => new { pm.ProjectId, pm.UserId });

        modelBuilder.Entity<ProjectMember>()
            .HasOne(pm => pm.Project)
            .WithMany(p => p.TeamMembers)
            .HasForeignKey(pm => pm.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ProjectMember>()
            .HasOne(pm => pm.User)
            .WithMany(u => u.ProjectMembers) // secondary mapping
            .HasForeignKey(pm => pm.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ProjectMember>()
            .HasOne(pm => pm.InvitationStatusMaster)
            .WithMany()
            .HasForeignKey(pm => pm.InvitationStatusId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<ProjectMember>()
            .HasOne(pm => pm.PreviousInvitationStatusMaster)
            .WithMany()
            .HasForeignKey(pm => pm.PreviousInvitationStatusId)
            .OnDelete(DeleteBehavior.NoAction);

        // Milestone Relations
        modelBuilder.Entity<Milestone>()
            .HasOne(m => m.Project)
            .WithMany(p => p.Milestones)
            .HasForeignKey(m => m.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Milestone>()
            .HasOne(m => m.Owner)
            .WithMany()
            .HasForeignKey(m => m.OwnerId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Milestone>()
            .HasOne(m => m.StatusMaster)
            .WithMany()
            .HasForeignKey(m => m.StatusId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Milestone>()
            .HasOne(m => m.PriorityMaster)
            .WithMany()
            .HasForeignKey(m => m.PriorityId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Milestone>()
            .HasOne(m => m.PreviousStatusMaster)
            .WithMany()
            .HasForeignKey(m => m.PreviousStatusId)
            .OnDelete(DeleteBehavior.NoAction);

        // TaskList Relations
        modelBuilder.Entity<TaskList>()
            .HasOne(tl => tl.Project)
            .WithMany(p => p.TaskLists)
            .HasForeignKey(tl => tl.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TaskList>()
            .HasOne(tl => tl.Milestone)
            .WithMany(m => m.TaskLists)
            .HasForeignKey(tl => tl.MilestoneId)
            .OnDelete(DeleteBehavior.NoAction);

        // Task Relations
        modelBuilder.Entity<Task>()
            .HasOne(t => t.Project)
            .WithMany(p => p.Tasks)
            .HasForeignKey(t => t.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Task>()
            .HasOne(t => t.TaskList)
            .WithMany(tl => tl.Tasks)
            .HasForeignKey(t => t.TaskListId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Task>()
            .HasOne(t => t.Milestone)
            .WithMany()
            .HasForeignKey(t => t.MilestoneId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Task>()
            .HasOne(t => t.AssociatedTeam)
            .WithMany()
            .HasForeignKey(t => t.AssociatedTeamId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Task>()
            .HasOne(t => t.Assignee)
            .WithMany()
            .HasForeignKey(t => t.AssigneeId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Task>()
            .HasOne(t => t.Creator)
            .WithMany()
            .HasForeignKey(t => t.CreatedById)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Task>()
            .HasOne(t => t.SingleOwner)
            .WithMany()
            .HasForeignKey(t => t.OwnerId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Task>()
            .HasOne(t => t.StatusMaster)
            .WithMany()
            .HasForeignKey(t => t.StatusId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Task>()
            .HasOne(t => t.PriorityMaster)
            .WithMany()
            .HasForeignKey(t => t.PriorityId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Task>()
            .HasOne(t => t.PreviousStatusMaster)
            .WithMany()
            .HasForeignKey(t => t.PreviousStatusId)
            .OnDelete(DeleteBehavior.NoAction);

        // Issue Relations
        modelBuilder.Entity<Issue>()
            .HasOne(i => i.Project)
            .WithMany(p => p.Issues)
            .HasForeignKey(i => i.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Issue>()
            .HasOne(i => i.Milestone)
            .WithMany(m => m.Issues)
            .HasForeignKey(i => i.MilestoneId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Issue>()
            .HasOne(i => i.AssociatedTeam)
            .WithMany()
            .HasForeignKey(i => i.AssociatedTeamId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Issue>()
            .HasOne(i => i.Reporter)
            .WithMany()
            .HasForeignKey(i => i.ReporterId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Issue>()
            .HasOne(i => i.Assignee)
            .WithMany()
            .HasForeignKey(i => i.AssigneeId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Issue>()
            .HasOne(i => i.StatusMaster)
            .WithMany()
            .HasForeignKey(i => i.StatusId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Issue>()
            .HasOne(i => i.PriorityMaster)
            .WithMany()
            .HasForeignKey(i => i.PriorityId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Issue>()
            .HasOne(i => i.SeverityMaster)
            .WithMany()
            .HasForeignKey(i => i.SeverityId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Issue>()
            .HasOne(i => i.ClassificationMaster)
            .WithMany()
            .HasForeignKey(i => i.ClassificationId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Issue>()
            .HasOne(i => i.PreviousStatusMaster)
            .WithMany()
            .HasForeignKey(i => i.PreviousStatusId)
            .OnDelete(DeleteBehavior.NoAction);

        // TimeLog Relations
        modelBuilder.Entity<TimeLog>()
            .HasOne(tl => tl.User)
            .WithMany()
            .HasForeignKey(tl => tl.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TimeLog>()
            .HasOne(tl => tl.CreatedBy)
            .WithMany()
            .HasForeignKey(tl => tl.CreatedById)
            .OnDelete(DeleteBehavior.NoAction);

        // Project -> Tasks is already Cascade, and Tasks -> TimeLogs is Cascade below, so a
        // *direct* Project -> TimeLogs cascade too would be a second cascade path to the
        // same table (SQL Server error 1785, same root cause as issue_document_link above).
        // Keep the Task edge cascading (the more specific owner when a log is task-scoped);
        // a project-level "general log" with no TaskId is reached through Project only, so
        // NoAction here, not Cascade.
        modelBuilder.Entity<TimeLog>()
            .HasOne(tl => tl.Project)
            .WithMany(p => p.Timelogs)
            .HasForeignKey(tl => tl.ProjectId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<TimeLog>()
            .HasOne(tl => tl.Task)
            .WithMany(t => t.Timelogs)
            .HasForeignKey(tl => tl.TaskId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TimeLog>()
            .HasOne(tl => tl.Issue)
            .WithMany()
            .HasForeignKey(tl => tl.IssueId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<TimeLog>()
            .HasOne(tl => tl.Timesheet)
            .WithMany(ts => ts.Timelogs)
            .HasForeignKey(tl => tl.TimesheetId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<TimeLog>()
            .HasOne(tl => tl.ApprovalStatusMaster)
            .WithMany()
            .HasForeignKey(tl => tl.ApprovalStatusId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<TimeLog>()
            .HasOne(tl => tl.PreviousApprovalStatusMaster)
            .WithMany()
            .HasForeignKey(tl => tl.PreviousApprovalStatusId)
            .OnDelete(DeleteBehavior.NoAction);

        // Timesheet Relations
        modelBuilder.Entity<Timesheet>()
            .HasOne(ts => ts.Project)
            .WithMany()
            .HasForeignKey(ts => ts.ProjectId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Timesheet>()
            .HasOne(ts => ts.User)
            .WithMany()
            .HasForeignKey(ts => ts.UserEmail)
            .HasPrincipalKey(u => u.Email)
            .OnDelete(DeleteBehavior.NoAction);

        // Project chat: messages live and die with their project; deleting a user keeps their
        // messages (sender becomes null) but drops their read markers.
        modelBuilder.Entity<ChatMessage>()
            .HasOne(m => m.Project).WithMany().HasForeignKey(m => m.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<ChatMessage>()
            .HasOne(m => m.Sender).WithMany().HasForeignKey(m => m.SenderId)
            .OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<ChatMessage>().Property(m => m.Body).HasMaxLength(4000);
        modelBuilder.Entity<ChatMessage>().HasIndex(m => new { m.ProjectId, m.Id });

        modelBuilder.Entity<ChatMention>()
            .HasOne(x => x.Message).WithMany().HasForeignKey(x => x.MessageId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<ChatMention>()
            .HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<ChatMention>().HasIndex(x => new { x.MessageId, x.UserId }).IsUnique();
        modelBuilder.Entity<ChatMention>().HasIndex(x => x.UserId);

        modelBuilder.Entity<ChatAttachment>()
            .HasOne(a => a.Message).WithMany().HasForeignKey(a => a.MessageId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<ChatAttachment>().Property(a => a.FileName).HasMaxLength(255);
        modelBuilder.Entity<ChatAttachment>().Property(a => a.ContentType).HasMaxLength(100);
        modelBuilder.Entity<ChatAttachment>().HasIndex(a => a.MessageId);

        modelBuilder.Entity<ChatReadState>()
            .HasOne(r => r.Project).WithMany().HasForeignKey(r => r.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<ChatReadState>()
            .HasOne(r => r.User).WithMany().HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<ChatReadState>().HasIndex(r => new { r.ProjectId, r.UserId }).IsUnique();

        // Document Relations
        modelBuilder.Entity<Document>()
            .HasOne(d => d.Project)
            .WithMany(p => p.Documents)
            .HasForeignKey(d => d.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Document>()
            .HasOne(d => d.UploadedBy)
            .WithMany()
            .HasForeignKey(d => d.UploadedByEmail)
            .HasPrincipalKey(u => u.Email)
            .OnDelete(DeleteBehavior.NoAction);


        // --- MANY TO MANY JOIN TABLES MAPS ---

        // User <-> Team Link
        modelBuilder.Entity<User>()
            .HasMany(u => u.Teams)
            .WithMany(t => t.Members)
            .UsingEntity(j => j.ToTable("user_team_link"));

        // User <-> Skill Link
        modelBuilder.Entity<User>()
            .HasMany(u => u.Skills)
            .WithMany(s => s.Users)
            .UsingEntity(j => j.ToTable("user_skill_link"));

        // Task <-> Owners Link
        modelBuilder.Entity<Task>()
            .HasMany(t => t.Owners)
            .WithMany()
            .UsingEntity(j => j.ToTable("task_owners"));

        // Task <-> Assignees Link
        modelBuilder.Entity<Task>()
            .HasMany(t => t.Assignees)
            .WithMany()
            .UsingEntity(j => j.ToTable("task_assignees"));

        // Issue <-> Followers Link
        modelBuilder.Entity<Issue>()
            .HasMany(i => i.Followers)
            .WithMany()
            .UsingEntity(j => j.ToTable("issue_followers"));

        // Issue <-> Assignees Link
        modelBuilder.Entity<Issue>()
            .HasMany(i => i.Assignees)
            .WithMany()
            .UsingEntity(j => j.ToTable("issue_assignees"));

        // Issue <-> Document Link
        // Both Issue and Document already cascade-delete from Project directly, so letting
        // this join table cascade from *both* sides creates two cascade paths from Project
        // down to the same table — SQL Server rejects that (error 1785) at migration time.
        // NoAction on both sides here; a hard delete of an Issue/Document should clean up
        // its own link rows at the application layer, consistent with every other FK in
        // this model that isn't a direct single-path parent-child relationship.
        modelBuilder.Entity<Issue>()
            .HasMany(i => i.Documents)
            .WithMany(d => d.Issues)
            .UsingEntity<Dictionary<string, object>>(
                "issue_document_link",
                j => j.HasOne<Document>().WithMany().HasForeignKey("DocumentsId").OnDelete(DeleteBehavior.NoAction),
                j => j.HasOne<Issue>().WithMany().HasForeignKey("IssuesId").OnDelete(DeleteBehavior.NoAction),
                j => j.ToTable("issue_document_link"));

        // --- Project Templates ---
        modelBuilder.Entity<ProjectTemplate>().HasIndex(pt => pt.Name).IsUnique();

        modelBuilder.Entity<ProjectTemplate>()
            .HasOne(pt => pt.SourceProject)
            .WithMany()
            .HasForeignKey(pt => pt.SourceProjectId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<ProjectTemplate>()
            .HasOne(pt => pt.CreatedBy)
            .WithMany()
            .HasForeignKey(pt => pt.CreatedById)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<TemplateTask>()
            .HasOne(tt => tt.Template)
            .WithMany(pt => pt.Tasks)
            .HasForeignKey(tt => tt.TemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TemplateTask>()
            .HasIndex(tt => new { tt.TemplateId, tt.Title }).IsUnique();

        modelBuilder.Entity<TemplateTask>().Property(tt => tt.EstimatedHours).HasPrecision(10, 2);

        // Shadow FK: Project.TemplateId -> ProjectTemplate.Id (Project has no nav property
        // for this, matching how it was left in PMS.Domain/Entities/Project.cs).
        modelBuilder.Entity<Project>()
            .HasOne<ProjectTemplate>()
            .WithMany()
            .HasForeignKey(p => p.TemplateId)
            .OnDelete(DeleteBehavior.NoAction);

        // --- Audit tables: explicit keys (source uses "ID"/"Id" casing EF's convention
        // doesn't reliably auto-detect the same way across all four) ---
        modelBuilder.Entity<AuditFieldsMapping>().HasKey(a => a.ID);
        modelBuilder.Entity<AuditLogs>().HasKey(a => a.ID);
        modelBuilder.Entity<AuditLogDetails>().HasKey(a => a.Id);
        modelBuilder.Entity<AuditMetaDataInfo>().HasKey(a => a.Id);

        // EF pluralizes table names from the DbSet property by default
        // (AuditFieldsMappings, AuditMetaDataInfos) — source uses singular for these two.
        modelBuilder.Entity<AuditFieldsMapping>().ToTable("AuditFieldsMapping");
        modelBuilder.Entity<AuditMetaDataInfo>().ToTable("AuditMetaDataInfo");

        modelBuilder.Entity<AuditLogDetails>()
            .HasOne(d => d.AuditLog)
            .WithMany(l => l.Details)
            .HasForeignKey(d => d.AuditLogId)
            .OnDelete(DeleteBehavior.Cascade);

        // --- DECIMAL PRECISION: match the source schema's Numeric(p, s) exactly ---
        // Left unspecified, EF Core/SQL Server defaults every `decimal` column to (18, 2),
        // silently truncating anything that doesn't fit. These match PMS-backend's
        // SQLAlchemy column definitions column-for-column (see SOURCE_DATABASE_SCHEMA.md).
        modelBuilder.Entity<Project>().Property(p => p.EstimatedHours).HasPrecision(10, 2);
        modelBuilder.Entity<Project>().Property(p => p.ActualHours).HasPrecision(10, 2);
        modelBuilder.Entity<Task>().Property(t => t.EstimatedHours).HasPrecision(10, 2);
        modelBuilder.Entity<Task>().Property(t => t.WorkHours).HasPrecision(10, 2);
        modelBuilder.Entity<Task>().Property(t => t.CachedTimelogTotal).HasPrecision(10, 2);
        modelBuilder.Entity<Issue>().Property(i => i.EstimatedHours).HasPrecision(10, 2);
        modelBuilder.Entity<TimeLog>().Property(tl => tl.DailyLogHours).HasPrecision(10, 2);
        modelBuilder.Entity<Team>().Property(t => t.BudgetAllocation).HasPrecision(12, 2);
        modelBuilder.Entity<Timesheet>().Property(ts => ts.TotalHours).HasPrecision(5, 2);

        // --- TABLE NAMING: force every table name to snake_case lowercase ---
        // EF's default convention names a table after its DbSet/entity ("Users", "Projects",
        // "TaskLists", ...) — PascalCase. The source (MySQL) schema is entirely lowercase
        // snake_case ("users", "projects", "task_lists", ...), and the join tables above are
        // already configured that way by hand. This loop makes every *other* table match the
        // same convention instead of mixing PascalCase entity tables with snake_case join
        // tables. Column names are untouched — only table names.
        //
        // Exception: the four Audit* tables are already modeled PascalCase *in the source
        // schema itself* (see SOURCE_DATABASE_SCHEMA.md, "Audit tables — already MSSQL-shaped
        // in source") — preserve that, don't "fix" it to match everything else.
        var auditEntityClrTypes = new HashSet<Type>
        {
            typeof(AuditFieldsMapping), typeof(AuditLogs), typeof(AuditLogDetails), typeof(AuditMetaDataInfo)
        };
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (auditEntityClrTypes.Contains(entityType.ClrType)) continue;

            var tableName = entityType.GetTableName();
            if (tableName != null)
            {
                entityType.SetTableName(ToSnakeCase(tableName));
            }
        }
    }

    private static string ToSnakeCase(string value)
    {
        var sb = new System.Text.StringBuilder();
        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];
            if (char.IsUpper(c))
            {
                if (i > 0 && (char.IsLower(value[i - 1]) || (i + 1 < value.Length && char.IsLower(value[i + 1]))))
                {
                    sb.Append('_');
                }
                sb.Append(char.ToLowerInvariant(c));
            }
            else
            {
                sb.Append(c);
            }
        }
        return sb.ToString();
    }
}
