using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MoveToPmsSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "pms");

            migrationBuilder.RenameTable(
                name: "users",
                newName: "users",
                newSchema: "pms");

            migrationBuilder.RenameTable(
                name: "user_team_link",
                newName: "user_team_link",
                newSchema: "pms");

            migrationBuilder.RenameTable(
                name: "user_statuses",
                newName: "user_statuses",
                newSchema: "pms");

            migrationBuilder.RenameTable(
                name: "user_skill_link",
                newName: "user_skill_link",
                newSchema: "pms");

            migrationBuilder.RenameTable(
                name: "timesheets",
                newName: "timesheets",
                newSchema: "pms");

            migrationBuilder.RenameTable(
                name: "timelogs",
                newName: "timelogs",
                newSchema: "pms");

            migrationBuilder.RenameTable(
                name: "template_tasks",
                newName: "template_tasks",
                newSchema: "pms");

            migrationBuilder.RenameTable(
                name: "teams",
                newName: "teams",
                newSchema: "pms");

            migrationBuilder.RenameTable(
                name: "tasks",
                newName: "tasks",
                newSchema: "pms");

            migrationBuilder.RenameTable(
                name: "task_owners",
                newName: "task_owners",
                newSchema: "pms");

            migrationBuilder.RenameTable(
                name: "task_lists",
                newName: "task_lists",
                newSchema: "pms");

            migrationBuilder.RenameTable(
                name: "task_assignees",
                newName: "task_assignees",
                newSchema: "pms");

            migrationBuilder.RenameTable(
                name: "skills",
                newName: "skills",
                newSchema: "pms");

            migrationBuilder.RenameTable(
                name: "roles",
                newName: "roles",
                newSchema: "pms");

            migrationBuilder.RenameTable(
                name: "projects",
                newName: "projects",
                newSchema: "pms");

            migrationBuilder.RenameTable(
                name: "project_templates",
                newName: "project_templates",
                newSchema: "pms");

            migrationBuilder.RenameTable(
                name: "project_members",
                newName: "project_members",
                newSchema: "pms");

            migrationBuilder.RenameTable(
                name: "project_groups",
                newName: "project_groups",
                newSchema: "pms");

            migrationBuilder.RenameTable(
                name: "milestones",
                newName: "milestones",
                newSchema: "pms");

            migrationBuilder.RenameTable(
                name: "master_lookups",
                newName: "master_lookups",
                newSchema: "pms");

            migrationBuilder.RenameTable(
                name: "issues",
                newName: "issues",
                newSchema: "pms");

            migrationBuilder.RenameTable(
                name: "issue_followers",
                newName: "issue_followers",
                newSchema: "pms");

            migrationBuilder.RenameTable(
                name: "issue_document_link",
                newName: "issue_document_link",
                newSchema: "pms");

            migrationBuilder.RenameTable(
                name: "issue_assignees",
                newName: "issue_assignees",
                newSchema: "pms");

            migrationBuilder.RenameTable(
                name: "documents",
                newName: "documents",
                newSchema: "pms");

            migrationBuilder.RenameTable(
                name: "chat_read_states",
                newName: "chat_read_states",
                newSchema: "pms");

            migrationBuilder.RenameTable(
                name: "chat_messages",
                newName: "chat_messages",
                newSchema: "pms");

            migrationBuilder.RenameTable(
                name: "chat_mentions",
                newName: "chat_mentions",
                newSchema: "pms");

            migrationBuilder.RenameTable(
                name: "chat_attachments",
                newName: "chat_attachments",
                newSchema: "pms");

            migrationBuilder.RenameTable(
                name: "AuditMetaDataInfo",
                newName: "AuditMetaDataInfo",
                newSchema: "pms");

            migrationBuilder.RenameTable(
                name: "AuditLogs",
                newName: "AuditLogs",
                newSchema: "pms");

            migrationBuilder.RenameTable(
                name: "AuditLogDetails",
                newName: "AuditLogDetails",
                newSchema: "pms");

            migrationBuilder.RenameTable(
                name: "AuditFieldsMapping",
                newName: "AuditFieldsMapping",
                newSchema: "pms");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "users",
                schema: "pms",
                newName: "users");

            migrationBuilder.RenameTable(
                name: "user_team_link",
                schema: "pms",
                newName: "user_team_link");

            migrationBuilder.RenameTable(
                name: "user_statuses",
                schema: "pms",
                newName: "user_statuses");

            migrationBuilder.RenameTable(
                name: "user_skill_link",
                schema: "pms",
                newName: "user_skill_link");

            migrationBuilder.RenameTable(
                name: "timesheets",
                schema: "pms",
                newName: "timesheets");

            migrationBuilder.RenameTable(
                name: "timelogs",
                schema: "pms",
                newName: "timelogs");

            migrationBuilder.RenameTable(
                name: "template_tasks",
                schema: "pms",
                newName: "template_tasks");

            migrationBuilder.RenameTable(
                name: "teams",
                schema: "pms",
                newName: "teams");

            migrationBuilder.RenameTable(
                name: "tasks",
                schema: "pms",
                newName: "tasks");

            migrationBuilder.RenameTable(
                name: "task_owners",
                schema: "pms",
                newName: "task_owners");

            migrationBuilder.RenameTable(
                name: "task_lists",
                schema: "pms",
                newName: "task_lists");

            migrationBuilder.RenameTable(
                name: "task_assignees",
                schema: "pms",
                newName: "task_assignees");

            migrationBuilder.RenameTable(
                name: "skills",
                schema: "pms",
                newName: "skills");

            migrationBuilder.RenameTable(
                name: "roles",
                schema: "pms",
                newName: "roles");

            migrationBuilder.RenameTable(
                name: "projects",
                schema: "pms",
                newName: "projects");

            migrationBuilder.RenameTable(
                name: "project_templates",
                schema: "pms",
                newName: "project_templates");

            migrationBuilder.RenameTable(
                name: "project_members",
                schema: "pms",
                newName: "project_members");

            migrationBuilder.RenameTable(
                name: "project_groups",
                schema: "pms",
                newName: "project_groups");

            migrationBuilder.RenameTable(
                name: "milestones",
                schema: "pms",
                newName: "milestones");

            migrationBuilder.RenameTable(
                name: "master_lookups",
                schema: "pms",
                newName: "master_lookups");

            migrationBuilder.RenameTable(
                name: "issues",
                schema: "pms",
                newName: "issues");

            migrationBuilder.RenameTable(
                name: "issue_followers",
                schema: "pms",
                newName: "issue_followers");

            migrationBuilder.RenameTable(
                name: "issue_document_link",
                schema: "pms",
                newName: "issue_document_link");

            migrationBuilder.RenameTable(
                name: "issue_assignees",
                schema: "pms",
                newName: "issue_assignees");

            migrationBuilder.RenameTable(
                name: "documents",
                schema: "pms",
                newName: "documents");

            migrationBuilder.RenameTable(
                name: "chat_read_states",
                schema: "pms",
                newName: "chat_read_states");

            migrationBuilder.RenameTable(
                name: "chat_messages",
                schema: "pms",
                newName: "chat_messages");

            migrationBuilder.RenameTable(
                name: "chat_mentions",
                schema: "pms",
                newName: "chat_mentions");

            migrationBuilder.RenameTable(
                name: "chat_attachments",
                schema: "pms",
                newName: "chat_attachments");

            migrationBuilder.RenameTable(
                name: "AuditMetaDataInfo",
                schema: "pms",
                newName: "AuditMetaDataInfo");

            migrationBuilder.RenameTable(
                name: "AuditLogs",
                schema: "pms",
                newName: "AuditLogs");

            migrationBuilder.RenameTable(
                name: "AuditLogDetails",
                schema: "pms",
                newName: "AuditLogDetails");

            migrationBuilder.RenameTable(
                name: "AuditFieldsMapping",
                schema: "pms",
                newName: "AuditFieldsMapping");
        }
    }
}
