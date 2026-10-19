using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameTablesToSnakeCase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Documents_Projects_ProjectId",
                table: "Documents");

            migrationBuilder.DropForeignKey(
                name: "FK_Documents_Users_UploadedByEmail",
                table: "Documents");

            migrationBuilder.DropForeignKey(
                name: "FK_issue_assignees_Issues_Issue1Id",
                table: "issue_assignees");

            migrationBuilder.DropForeignKey(
                name: "FK_issue_assignees_Users_AssigneesId",
                table: "issue_assignees");

            migrationBuilder.DropForeignKey(
                name: "FK_issue_document_link_Documents_DocumentsId",
                table: "issue_document_link");

            migrationBuilder.DropForeignKey(
                name: "FK_issue_document_link_Issues_IssuesId",
                table: "issue_document_link");

            migrationBuilder.DropForeignKey(
                name: "FK_issue_followers_Issues_IssueId",
                table: "issue_followers");

            migrationBuilder.DropForeignKey(
                name: "FK_issue_followers_Users_FollowersId",
                table: "issue_followers");

            migrationBuilder.DropForeignKey(
                name: "FK_Issues_MasterLookups_ClassificationId",
                table: "Issues");

            migrationBuilder.DropForeignKey(
                name: "FK_Issues_MasterLookups_PreviousStatusId",
                table: "Issues");

            migrationBuilder.DropForeignKey(
                name: "FK_Issues_MasterLookups_PriorityId",
                table: "Issues");

            migrationBuilder.DropForeignKey(
                name: "FK_Issues_MasterLookups_SeverityId",
                table: "Issues");

            migrationBuilder.DropForeignKey(
                name: "FK_Issues_MasterLookups_StatusId",
                table: "Issues");

            migrationBuilder.DropForeignKey(
                name: "FK_Issues_Milestones_MilestoneId",
                table: "Issues");

            migrationBuilder.DropForeignKey(
                name: "FK_Issues_Projects_ProjectId",
                table: "Issues");

            migrationBuilder.DropForeignKey(
                name: "FK_Issues_Teams_AssociatedTeamId",
                table: "Issues");

            migrationBuilder.DropForeignKey(
                name: "FK_Issues_Users_AssigneeId",
                table: "Issues");

            migrationBuilder.DropForeignKey(
                name: "FK_Issues_Users_ReporterId",
                table: "Issues");

            migrationBuilder.DropForeignKey(
                name: "FK_Milestones_MasterLookups_PreviousStatusId",
                table: "Milestones");

            migrationBuilder.DropForeignKey(
                name: "FK_Milestones_MasterLookups_PriorityId",
                table: "Milestones");

            migrationBuilder.DropForeignKey(
                name: "FK_Milestones_MasterLookups_StatusId",
                table: "Milestones");

            migrationBuilder.DropForeignKey(
                name: "FK_Milestones_Projects_ProjectId",
                table: "Milestones");

            migrationBuilder.DropForeignKey(
                name: "FK_Milestones_Users_OwnerId",
                table: "Milestones");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectMembers_MasterLookups_InvitationStatusId",
                table: "ProjectMembers");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectMembers_MasterLookups_PreviousInvitationStatusId",
                table: "ProjectMembers");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectMembers_Projects_ProjectId",
                table: "ProjectMembers");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectMembers_Users_UserId",
                table: "ProjectMembers");

            migrationBuilder.DropForeignKey(
                name: "FK_Projects_MasterLookups_PreviousStatusId",
                table: "Projects");

            migrationBuilder.DropForeignKey(
                name: "FK_Projects_MasterLookups_PriorityId",
                table: "Projects");

            migrationBuilder.DropForeignKey(
                name: "FK_Projects_MasterLookups_StatusId",
                table: "Projects");

            migrationBuilder.DropForeignKey(
                name: "FK_Projects_Users_DeliveryHeadId",
                table: "Projects");

            migrationBuilder.DropForeignKey(
                name: "FK_Projects_Users_OwnerId",
                table: "Projects");

            migrationBuilder.DropForeignKey(
                name: "FK_Projects_Users_ProjectManagerId",
                table: "Projects");

            migrationBuilder.DropForeignKey(
                name: "FK_task_assignees_Tasks_Task1Id",
                table: "task_assignees");

            migrationBuilder.DropForeignKey(
                name: "FK_task_assignees_Users_AssigneesId",
                table: "task_assignees");

            migrationBuilder.DropForeignKey(
                name: "FK_task_owners_Tasks_TaskId",
                table: "task_owners");

            migrationBuilder.DropForeignKey(
                name: "FK_task_owners_Users_OwnersId",
                table: "task_owners");

            migrationBuilder.DropForeignKey(
                name: "FK_TaskLists_Milestones_MilestoneId",
                table: "TaskLists");

            migrationBuilder.DropForeignKey(
                name: "FK_TaskLists_Projects_ProjectId",
                table: "TaskLists");

            migrationBuilder.DropForeignKey(
                name: "FK_Tasks_MasterLookups_PreviousStatusId",
                table: "Tasks");

            migrationBuilder.DropForeignKey(
                name: "FK_Tasks_MasterLookups_PriorityId",
                table: "Tasks");

            migrationBuilder.DropForeignKey(
                name: "FK_Tasks_MasterLookups_StatusId",
                table: "Tasks");

            migrationBuilder.DropForeignKey(
                name: "FK_Tasks_Milestones_MilestoneId",
                table: "Tasks");

            migrationBuilder.DropForeignKey(
                name: "FK_Tasks_Projects_ProjectId",
                table: "Tasks");

            migrationBuilder.DropForeignKey(
                name: "FK_Tasks_TaskLists_TaskListId",
                table: "Tasks");

            migrationBuilder.DropForeignKey(
                name: "FK_Tasks_Teams_AssociatedTeamId",
                table: "Tasks");

            migrationBuilder.DropForeignKey(
                name: "FK_Tasks_Users_AssigneeId",
                table: "Tasks");

            migrationBuilder.DropForeignKey(
                name: "FK_Tasks_Users_CreatedById",
                table: "Tasks");

            migrationBuilder.DropForeignKey(
                name: "FK_Tasks_Users_OwnerId",
                table: "Tasks");

            migrationBuilder.DropForeignKey(
                name: "FK_Teams_Users_LeadEmail",
                table: "Teams");

            migrationBuilder.DropForeignKey(
                name: "FK_Timelogs_Issues_IssueId",
                table: "Timelogs");

            migrationBuilder.DropForeignKey(
                name: "FK_Timelogs_MasterLookups_ApprovalStatusId",
                table: "Timelogs");

            migrationBuilder.DropForeignKey(
                name: "FK_Timelogs_MasterLookups_PreviousApprovalStatusId",
                table: "Timelogs");

            migrationBuilder.DropForeignKey(
                name: "FK_Timelogs_Projects_ProjectId",
                table: "Timelogs");

            migrationBuilder.DropForeignKey(
                name: "FK_Timelogs_Tasks_TaskId",
                table: "Timelogs");

            migrationBuilder.DropForeignKey(
                name: "FK_Timelogs_Timesheets_TimesheetId",
                table: "Timelogs");

            migrationBuilder.DropForeignKey(
                name: "FK_Timelogs_Users_CreatedById",
                table: "Timelogs");

            migrationBuilder.DropForeignKey(
                name: "FK_Timelogs_Users_UserId",
                table: "Timelogs");

            migrationBuilder.DropForeignKey(
                name: "FK_Timesheets_Projects_ProjectId",
                table: "Timesheets");

            migrationBuilder.DropForeignKey(
                name: "FK_Timesheets_Users_UserEmail",
                table: "Timesheets");

            migrationBuilder.DropForeignKey(
                name: "FK_user_skill_link_Skills_SkillsId",
                table: "user_skill_link");

            migrationBuilder.DropForeignKey(
                name: "FK_user_skill_link_Users_UsersId",
                table: "user_skill_link");

            migrationBuilder.DropForeignKey(
                name: "FK_user_team_link_Teams_TeamsId",
                table: "user_team_link");

            migrationBuilder.DropForeignKey(
                name: "FK_user_team_link_Users_MembersId",
                table: "user_team_link");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Roles_RoleId",
                table: "Users");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_UserStatuses_StatusId",
                table: "Users");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Users_ManagerEmail",
                table: "Users");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Users_Email",
                table: "Users");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Users",
                table: "Users");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Timesheets",
                table: "Timesheets");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Timelogs",
                table: "Timelogs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Teams",
                table: "Teams");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Tasks",
                table: "Tasks");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Skills",
                table: "Skills");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Roles",
                table: "Roles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Projects",
                table: "Projects");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Milestones",
                table: "Milestones");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Issues",
                table: "Issues");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Documents",
                table: "Documents");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserStatuses",
                table: "UserStatuses");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TaskLists",
                table: "TaskLists");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ProjectMembers",
                table: "ProjectMembers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ProjectGroups",
                table: "ProjectGroups");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MasterLookups",
                table: "MasterLookups");

            migrationBuilder.RenameTable(
                name: "Users",
                newName: "users");

            migrationBuilder.RenameTable(
                name: "Timesheets",
                newName: "timesheets");

            migrationBuilder.RenameTable(
                name: "Timelogs",
                newName: "timelogs");

            migrationBuilder.RenameTable(
                name: "Teams",
                newName: "teams");

            migrationBuilder.RenameTable(
                name: "Tasks",
                newName: "tasks");

            migrationBuilder.RenameTable(
                name: "Skills",
                newName: "skills");

            migrationBuilder.RenameTable(
                name: "Roles",
                newName: "roles");

            migrationBuilder.RenameTable(
                name: "Projects",
                newName: "projects");

            migrationBuilder.RenameTable(
                name: "Milestones",
                newName: "milestones");

            migrationBuilder.RenameTable(
                name: "Issues",
                newName: "issues");

            migrationBuilder.RenameTable(
                name: "Documents",
                newName: "documents");

            migrationBuilder.RenameTable(
                name: "UserStatuses",
                newName: "user_statuses");

            migrationBuilder.RenameTable(
                name: "TaskLists",
                newName: "task_lists");

            migrationBuilder.RenameTable(
                name: "ProjectMembers",
                newName: "project_members");

            migrationBuilder.RenameTable(
                name: "ProjectGroups",
                newName: "project_groups");

            migrationBuilder.RenameTable(
                name: "MasterLookups",
                newName: "master_lookups");

            migrationBuilder.RenameIndex(
                name: "IX_Users_Username",
                table: "users",
                newName: "IX_users_Username");

            migrationBuilder.RenameIndex(
                name: "IX_Users_StatusId",
                table: "users",
                newName: "IX_users_StatusId");

            migrationBuilder.RenameIndex(
                name: "IX_Users_RoleId",
                table: "users",
                newName: "IX_users_RoleId");

            migrationBuilder.RenameIndex(
                name: "IX_Users_PublicId",
                table: "users",
                newName: "IX_users_PublicId");

            migrationBuilder.RenameIndex(
                name: "IX_Users_O365Id",
                table: "users",
                newName: "IX_users_O365Id");

            migrationBuilder.RenameIndex(
                name: "IX_Users_ManagerEmail",
                table: "users",
                newName: "IX_users_ManagerEmail");

            migrationBuilder.RenameIndex(
                name: "IX_Users_EmployeeId",
                table: "users",
                newName: "IX_users_EmployeeId");

            migrationBuilder.RenameIndex(
                name: "IX_Users_Email",
                table: "users",
                newName: "IX_users_Email");

            migrationBuilder.RenameIndex(
                name: "IX_Timesheets_UserEmail",
                table: "timesheets",
                newName: "IX_timesheets_UserEmail");

            migrationBuilder.RenameIndex(
                name: "IX_Timesheets_ProjectId",
                table: "timesheets",
                newName: "IX_timesheets_ProjectId");

            migrationBuilder.RenameIndex(
                name: "IX_Timelogs_UserId",
                table: "timelogs",
                newName: "IX_timelogs_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_Timelogs_TimesheetId",
                table: "timelogs",
                newName: "IX_timelogs_TimesheetId");

            migrationBuilder.RenameIndex(
                name: "IX_Timelogs_TaskId",
                table: "timelogs",
                newName: "IX_timelogs_TaskId");

            migrationBuilder.RenameIndex(
                name: "IX_Timelogs_ProjectId",
                table: "timelogs",
                newName: "IX_timelogs_ProjectId");

            migrationBuilder.RenameIndex(
                name: "IX_Timelogs_PreviousApprovalStatusId",
                table: "timelogs",
                newName: "IX_timelogs_PreviousApprovalStatusId");

            migrationBuilder.RenameIndex(
                name: "IX_Timelogs_IssueId",
                table: "timelogs",
                newName: "IX_timelogs_IssueId");

            migrationBuilder.RenameIndex(
                name: "IX_Timelogs_CreatedById",
                table: "timelogs",
                newName: "IX_timelogs_CreatedById");

            migrationBuilder.RenameIndex(
                name: "IX_Timelogs_ApprovalStatusId",
                table: "timelogs",
                newName: "IX_timelogs_ApprovalStatusId");

            migrationBuilder.RenameIndex(
                name: "IX_Teams_TeamEmail",
                table: "teams",
                newName: "IX_teams_TeamEmail");

            migrationBuilder.RenameIndex(
                name: "IX_Teams_PublicId",
                table: "teams",
                newName: "IX_teams_PublicId");

            migrationBuilder.RenameIndex(
                name: "IX_Teams_LeadEmail",
                table: "teams",
                newName: "IX_teams_LeadEmail");

            migrationBuilder.RenameIndex(
                name: "IX_Tasks_TaskListId",
                table: "tasks",
                newName: "IX_tasks_TaskListId");

            migrationBuilder.RenameIndex(
                name: "IX_Tasks_StatusId",
                table: "tasks",
                newName: "IX_tasks_StatusId");

            migrationBuilder.RenameIndex(
                name: "IX_Tasks_PublicId",
                table: "tasks",
                newName: "IX_tasks_PublicId");

            migrationBuilder.RenameIndex(
                name: "IX_Tasks_ProjectId",
                table: "tasks",
                newName: "IX_tasks_ProjectId");

            migrationBuilder.RenameIndex(
                name: "IX_Tasks_PriorityId",
                table: "tasks",
                newName: "IX_tasks_PriorityId");

            migrationBuilder.RenameIndex(
                name: "IX_Tasks_PreviousStatusId",
                table: "tasks",
                newName: "IX_tasks_PreviousStatusId");

            migrationBuilder.RenameIndex(
                name: "IX_Tasks_OwnerId",
                table: "tasks",
                newName: "IX_tasks_OwnerId");

            migrationBuilder.RenameIndex(
                name: "IX_Tasks_MilestoneId",
                table: "tasks",
                newName: "IX_tasks_MilestoneId");

            migrationBuilder.RenameIndex(
                name: "IX_Tasks_CreatedById",
                table: "tasks",
                newName: "IX_tasks_CreatedById");

            migrationBuilder.RenameIndex(
                name: "IX_Tasks_AssociatedTeamId",
                table: "tasks",
                newName: "IX_tasks_AssociatedTeamId");

            migrationBuilder.RenameIndex(
                name: "IX_Tasks_AssigneeId",
                table: "tasks",
                newName: "IX_tasks_AssigneeId");

            migrationBuilder.RenameIndex(
                name: "IX_Skills_Name",
                table: "skills",
                newName: "IX_skills_Name");

            migrationBuilder.RenameIndex(
                name: "IX_Roles_Name",
                table: "roles",
                newName: "IX_roles_Name");

            migrationBuilder.RenameIndex(
                name: "IX_Projects_StatusId",
                table: "projects",
                newName: "IX_projects_StatusId");

            migrationBuilder.RenameIndex(
                name: "IX_Projects_PublicId",
                table: "projects",
                newName: "IX_projects_PublicId");

            migrationBuilder.RenameIndex(
                name: "IX_Projects_ProjectName",
                table: "projects",
                newName: "IX_projects_ProjectName");

            migrationBuilder.RenameIndex(
                name: "IX_Projects_ProjectManagerId",
                table: "projects",
                newName: "IX_projects_ProjectManagerId");

            migrationBuilder.RenameIndex(
                name: "IX_Projects_ProjectIdSync",
                table: "projects",
                newName: "IX_projects_ProjectIdSync");

            migrationBuilder.RenameIndex(
                name: "IX_Projects_PriorityId",
                table: "projects",
                newName: "IX_projects_PriorityId");

            migrationBuilder.RenameIndex(
                name: "IX_Projects_PreviousStatusId",
                table: "projects",
                newName: "IX_projects_PreviousStatusId");

            migrationBuilder.RenameIndex(
                name: "IX_Projects_OwnerId",
                table: "projects",
                newName: "IX_projects_OwnerId");

            migrationBuilder.RenameIndex(
                name: "IX_Projects_DeliveryHeadId",
                table: "projects",
                newName: "IX_projects_DeliveryHeadId");

            migrationBuilder.RenameIndex(
                name: "IX_Milestones_StatusId",
                table: "milestones",
                newName: "IX_milestones_StatusId");

            migrationBuilder.RenameIndex(
                name: "IX_Milestones_PublicId",
                table: "milestones",
                newName: "IX_milestones_PublicId");

            migrationBuilder.RenameIndex(
                name: "IX_Milestones_ProjectId",
                table: "milestones",
                newName: "IX_milestones_ProjectId");

            migrationBuilder.RenameIndex(
                name: "IX_Milestones_PriorityId",
                table: "milestones",
                newName: "IX_milestones_PriorityId");

            migrationBuilder.RenameIndex(
                name: "IX_Milestones_PreviousStatusId",
                table: "milestones",
                newName: "IX_milestones_PreviousStatusId");

            migrationBuilder.RenameIndex(
                name: "IX_Milestones_OwnerId",
                table: "milestones",
                newName: "IX_milestones_OwnerId");

            migrationBuilder.RenameIndex(
                name: "IX_Issues_StatusId",
                table: "issues",
                newName: "IX_issues_StatusId");

            migrationBuilder.RenameIndex(
                name: "IX_Issues_SeverityId",
                table: "issues",
                newName: "IX_issues_SeverityId");

            migrationBuilder.RenameIndex(
                name: "IX_Issues_ReporterId",
                table: "issues",
                newName: "IX_issues_ReporterId");

            migrationBuilder.RenameIndex(
                name: "IX_Issues_PublicId",
                table: "issues",
                newName: "IX_issues_PublicId");

            migrationBuilder.RenameIndex(
                name: "IX_Issues_ProjectId",
                table: "issues",
                newName: "IX_issues_ProjectId");

            migrationBuilder.RenameIndex(
                name: "IX_Issues_PriorityId",
                table: "issues",
                newName: "IX_issues_PriorityId");

            migrationBuilder.RenameIndex(
                name: "IX_Issues_PreviousStatusId",
                table: "issues",
                newName: "IX_issues_PreviousStatusId");

            migrationBuilder.RenameIndex(
                name: "IX_Issues_MilestoneId",
                table: "issues",
                newName: "IX_issues_MilestoneId");

            migrationBuilder.RenameIndex(
                name: "IX_Issues_ClassificationId",
                table: "issues",
                newName: "IX_issues_ClassificationId");

            migrationBuilder.RenameIndex(
                name: "IX_Issues_AssociatedTeamId",
                table: "issues",
                newName: "IX_issues_AssociatedTeamId");

            migrationBuilder.RenameIndex(
                name: "IX_Issues_AssigneeId",
                table: "issues",
                newName: "IX_issues_AssigneeId");

            migrationBuilder.RenameIndex(
                name: "IX_Documents_UploadedByEmail",
                table: "documents",
                newName: "IX_documents_UploadedByEmail");

            migrationBuilder.RenameIndex(
                name: "IX_Documents_ProjectId",
                table: "documents",
                newName: "IX_documents_ProjectId");

            migrationBuilder.RenameIndex(
                name: "IX_UserStatuses_Name",
                table: "user_statuses",
                newName: "IX_user_statuses_Name");

            migrationBuilder.RenameIndex(
                name: "IX_TaskLists_ProjectId",
                table: "task_lists",
                newName: "IX_task_lists_ProjectId");

            migrationBuilder.RenameIndex(
                name: "IX_TaskLists_MilestoneId",
                table: "task_lists",
                newName: "IX_task_lists_MilestoneId");

            migrationBuilder.RenameIndex(
                name: "IX_ProjectMembers_UserId",
                table: "project_members",
                newName: "IX_project_members_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_ProjectMembers_PreviousInvitationStatusId",
                table: "project_members",
                newName: "IX_project_members_PreviousInvitationStatusId");

            migrationBuilder.RenameIndex(
                name: "IX_ProjectMembers_InvitationStatusId",
                table: "project_members",
                newName: "IX_project_members_InvitationStatusId");

            migrationBuilder.RenameIndex(
                name: "IX_ProjectGroups_Name",
                table: "project_groups",
                newName: "IX_project_groups_Name");

            migrationBuilder.RenameIndex(
                name: "IX_MasterLookups_Category_Value",
                table: "master_lookups",
                newName: "IX_master_lookups_Category_Value");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_users_Email",
                table: "users",
                column: "Email");

            migrationBuilder.AddPrimaryKey(
                name: "PK_users",
                table: "users",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_timesheets",
                table: "timesheets",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_timelogs",
                table: "timelogs",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_teams",
                table: "teams",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_tasks",
                table: "tasks",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_skills",
                table: "skills",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_roles",
                table: "roles",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_projects",
                table: "projects",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_milestones",
                table: "milestones",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_issues",
                table: "issues",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_documents",
                table: "documents",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_user_statuses",
                table: "user_statuses",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_task_lists",
                table: "task_lists",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_project_members",
                table: "project_members",
                columns: new[] { "ProjectId", "UserId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_project_groups",
                table: "project_groups",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_master_lookups",
                table: "master_lookups",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_documents_projects_ProjectId",
                table: "documents",
                column: "ProjectId",
                principalTable: "projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_documents_users_UploadedByEmail",
                table: "documents",
                column: "UploadedByEmail",
                principalTable: "users",
                principalColumn: "Email");

            migrationBuilder.AddForeignKey(
                name: "FK_issue_assignees_issues_Issue1Id",
                table: "issue_assignees",
                column: "Issue1Id",
                principalTable: "issues",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_issue_assignees_users_AssigneesId",
                table: "issue_assignees",
                column: "AssigneesId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_issue_document_link_documents_DocumentsId",
                table: "issue_document_link",
                column: "DocumentsId",
                principalTable: "documents",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_issue_document_link_issues_IssuesId",
                table: "issue_document_link",
                column: "IssuesId",
                principalTable: "issues",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_issue_followers_issues_IssueId",
                table: "issue_followers",
                column: "IssueId",
                principalTable: "issues",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_issue_followers_users_FollowersId",
                table: "issue_followers",
                column: "FollowersId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_issues_master_lookups_ClassificationId",
                table: "issues",
                column: "ClassificationId",
                principalTable: "master_lookups",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_issues_master_lookups_PreviousStatusId",
                table: "issues",
                column: "PreviousStatusId",
                principalTable: "master_lookups",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_issues_master_lookups_PriorityId",
                table: "issues",
                column: "PriorityId",
                principalTable: "master_lookups",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_issues_master_lookups_SeverityId",
                table: "issues",
                column: "SeverityId",
                principalTable: "master_lookups",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_issues_master_lookups_StatusId",
                table: "issues",
                column: "StatusId",
                principalTable: "master_lookups",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_issues_milestones_MilestoneId",
                table: "issues",
                column: "MilestoneId",
                principalTable: "milestones",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_issues_projects_ProjectId",
                table: "issues",
                column: "ProjectId",
                principalTable: "projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_issues_teams_AssociatedTeamId",
                table: "issues",
                column: "AssociatedTeamId",
                principalTable: "teams",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_issues_users_AssigneeId",
                table: "issues",
                column: "AssigneeId",
                principalTable: "users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_issues_users_ReporterId",
                table: "issues",
                column: "ReporterId",
                principalTable: "users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_milestones_master_lookups_PreviousStatusId",
                table: "milestones",
                column: "PreviousStatusId",
                principalTable: "master_lookups",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_milestones_master_lookups_PriorityId",
                table: "milestones",
                column: "PriorityId",
                principalTable: "master_lookups",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_milestones_master_lookups_StatusId",
                table: "milestones",
                column: "StatusId",
                principalTable: "master_lookups",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_milestones_projects_ProjectId",
                table: "milestones",
                column: "ProjectId",
                principalTable: "projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_milestones_users_OwnerId",
                table: "milestones",
                column: "OwnerId",
                principalTable: "users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_project_members_master_lookups_InvitationStatusId",
                table: "project_members",
                column: "InvitationStatusId",
                principalTable: "master_lookups",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_project_members_master_lookups_PreviousInvitationStatusId",
                table: "project_members",
                column: "PreviousInvitationStatusId",
                principalTable: "master_lookups",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_project_members_projects_ProjectId",
                table: "project_members",
                column: "ProjectId",
                principalTable: "projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_project_members_users_UserId",
                table: "project_members",
                column: "UserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_projects_master_lookups_PreviousStatusId",
                table: "projects",
                column: "PreviousStatusId",
                principalTable: "master_lookups",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_projects_master_lookups_PriorityId",
                table: "projects",
                column: "PriorityId",
                principalTable: "master_lookups",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_projects_master_lookups_StatusId",
                table: "projects",
                column: "StatusId",
                principalTable: "master_lookups",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_projects_users_DeliveryHeadId",
                table: "projects",
                column: "DeliveryHeadId",
                principalTable: "users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_projects_users_OwnerId",
                table: "projects",
                column: "OwnerId",
                principalTable: "users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_projects_users_ProjectManagerId",
                table: "projects",
                column: "ProjectManagerId",
                principalTable: "users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_task_assignees_tasks_Task1Id",
                table: "task_assignees",
                column: "Task1Id",
                principalTable: "tasks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_task_assignees_users_AssigneesId",
                table: "task_assignees",
                column: "AssigneesId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_task_lists_milestones_MilestoneId",
                table: "task_lists",
                column: "MilestoneId",
                principalTable: "milestones",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_task_lists_projects_ProjectId",
                table: "task_lists",
                column: "ProjectId",
                principalTable: "projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_task_owners_tasks_TaskId",
                table: "task_owners",
                column: "TaskId",
                principalTable: "tasks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_task_owners_users_OwnersId",
                table: "task_owners",
                column: "OwnersId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_tasks_master_lookups_PreviousStatusId",
                table: "tasks",
                column: "PreviousStatusId",
                principalTable: "master_lookups",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_tasks_master_lookups_PriorityId",
                table: "tasks",
                column: "PriorityId",
                principalTable: "master_lookups",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_tasks_master_lookups_StatusId",
                table: "tasks",
                column: "StatusId",
                principalTable: "master_lookups",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_tasks_milestones_MilestoneId",
                table: "tasks",
                column: "MilestoneId",
                principalTable: "milestones",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_tasks_projects_ProjectId",
                table: "tasks",
                column: "ProjectId",
                principalTable: "projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_tasks_task_lists_TaskListId",
                table: "tasks",
                column: "TaskListId",
                principalTable: "task_lists",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_tasks_teams_AssociatedTeamId",
                table: "tasks",
                column: "AssociatedTeamId",
                principalTable: "teams",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_tasks_users_AssigneeId",
                table: "tasks",
                column: "AssigneeId",
                principalTable: "users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_tasks_users_CreatedById",
                table: "tasks",
                column: "CreatedById",
                principalTable: "users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_tasks_users_OwnerId",
                table: "tasks",
                column: "OwnerId",
                principalTable: "users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_teams_users_LeadEmail",
                table: "teams",
                column: "LeadEmail",
                principalTable: "users",
                principalColumn: "Email");

            migrationBuilder.AddForeignKey(
                name: "FK_timelogs_issues_IssueId",
                table: "timelogs",
                column: "IssueId",
                principalTable: "issues",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_timelogs_master_lookups_ApprovalStatusId",
                table: "timelogs",
                column: "ApprovalStatusId",
                principalTable: "master_lookups",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_timelogs_master_lookups_PreviousApprovalStatusId",
                table: "timelogs",
                column: "PreviousApprovalStatusId",
                principalTable: "master_lookups",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_timelogs_projects_ProjectId",
                table: "timelogs",
                column: "ProjectId",
                principalTable: "projects",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_timelogs_tasks_TaskId",
                table: "timelogs",
                column: "TaskId",
                principalTable: "tasks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_timelogs_timesheets_TimesheetId",
                table: "timelogs",
                column: "TimesheetId",
                principalTable: "timesheets",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_timelogs_users_CreatedById",
                table: "timelogs",
                column: "CreatedById",
                principalTable: "users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_timelogs_users_UserId",
                table: "timelogs",
                column: "UserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_timesheets_projects_ProjectId",
                table: "timesheets",
                column: "ProjectId",
                principalTable: "projects",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_timesheets_users_UserEmail",
                table: "timesheets",
                column: "UserEmail",
                principalTable: "users",
                principalColumn: "Email");

            migrationBuilder.AddForeignKey(
                name: "FK_user_skill_link_skills_SkillsId",
                table: "user_skill_link",
                column: "SkillsId",
                principalTable: "skills",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_user_skill_link_users_UsersId",
                table: "user_skill_link",
                column: "UsersId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_user_team_link_teams_TeamsId",
                table: "user_team_link",
                column: "TeamsId",
                principalTable: "teams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_user_team_link_users_MembersId",
                table: "user_team_link",
                column: "MembersId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_users_roles_RoleId",
                table: "users",
                column: "RoleId",
                principalTable: "roles",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_users_user_statuses_StatusId",
                table: "users",
                column: "StatusId",
                principalTable: "user_statuses",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_users_users_ManagerEmail",
                table: "users",
                column: "ManagerEmail",
                principalTable: "users",
                principalColumn: "Email");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_documents_projects_ProjectId",
                table: "documents");

            migrationBuilder.DropForeignKey(
                name: "FK_documents_users_UploadedByEmail",
                table: "documents");

            migrationBuilder.DropForeignKey(
                name: "FK_issue_assignees_issues_Issue1Id",
                table: "issue_assignees");

            migrationBuilder.DropForeignKey(
                name: "FK_issue_assignees_users_AssigneesId",
                table: "issue_assignees");

            migrationBuilder.DropForeignKey(
                name: "FK_issue_document_link_documents_DocumentsId",
                table: "issue_document_link");

            migrationBuilder.DropForeignKey(
                name: "FK_issue_document_link_issues_IssuesId",
                table: "issue_document_link");

            migrationBuilder.DropForeignKey(
                name: "FK_issue_followers_issues_IssueId",
                table: "issue_followers");

            migrationBuilder.DropForeignKey(
                name: "FK_issue_followers_users_FollowersId",
                table: "issue_followers");

            migrationBuilder.DropForeignKey(
                name: "FK_issues_master_lookups_ClassificationId",
                table: "issues");

            migrationBuilder.DropForeignKey(
                name: "FK_issues_master_lookups_PreviousStatusId",
                table: "issues");

            migrationBuilder.DropForeignKey(
                name: "FK_issues_master_lookups_PriorityId",
                table: "issues");

            migrationBuilder.DropForeignKey(
                name: "FK_issues_master_lookups_SeverityId",
                table: "issues");

            migrationBuilder.DropForeignKey(
                name: "FK_issues_master_lookups_StatusId",
                table: "issues");

            migrationBuilder.DropForeignKey(
                name: "FK_issues_milestones_MilestoneId",
                table: "issues");

            migrationBuilder.DropForeignKey(
                name: "FK_issues_projects_ProjectId",
                table: "issues");

            migrationBuilder.DropForeignKey(
                name: "FK_issues_teams_AssociatedTeamId",
                table: "issues");

            migrationBuilder.DropForeignKey(
                name: "FK_issues_users_AssigneeId",
                table: "issues");

            migrationBuilder.DropForeignKey(
                name: "FK_issues_users_ReporterId",
                table: "issues");

            migrationBuilder.DropForeignKey(
                name: "FK_milestones_master_lookups_PreviousStatusId",
                table: "milestones");

            migrationBuilder.DropForeignKey(
                name: "FK_milestones_master_lookups_PriorityId",
                table: "milestones");

            migrationBuilder.DropForeignKey(
                name: "FK_milestones_master_lookups_StatusId",
                table: "milestones");

            migrationBuilder.DropForeignKey(
                name: "FK_milestones_projects_ProjectId",
                table: "milestones");

            migrationBuilder.DropForeignKey(
                name: "FK_milestones_users_OwnerId",
                table: "milestones");

            migrationBuilder.DropForeignKey(
                name: "FK_project_members_master_lookups_InvitationStatusId",
                table: "project_members");

            migrationBuilder.DropForeignKey(
                name: "FK_project_members_master_lookups_PreviousInvitationStatusId",
                table: "project_members");

            migrationBuilder.DropForeignKey(
                name: "FK_project_members_projects_ProjectId",
                table: "project_members");

            migrationBuilder.DropForeignKey(
                name: "FK_project_members_users_UserId",
                table: "project_members");

            migrationBuilder.DropForeignKey(
                name: "FK_projects_master_lookups_PreviousStatusId",
                table: "projects");

            migrationBuilder.DropForeignKey(
                name: "FK_projects_master_lookups_PriorityId",
                table: "projects");

            migrationBuilder.DropForeignKey(
                name: "FK_projects_master_lookups_StatusId",
                table: "projects");

            migrationBuilder.DropForeignKey(
                name: "FK_projects_users_DeliveryHeadId",
                table: "projects");

            migrationBuilder.DropForeignKey(
                name: "FK_projects_users_OwnerId",
                table: "projects");

            migrationBuilder.DropForeignKey(
                name: "FK_projects_users_ProjectManagerId",
                table: "projects");

            migrationBuilder.DropForeignKey(
                name: "FK_task_assignees_tasks_Task1Id",
                table: "task_assignees");

            migrationBuilder.DropForeignKey(
                name: "FK_task_assignees_users_AssigneesId",
                table: "task_assignees");

            migrationBuilder.DropForeignKey(
                name: "FK_task_lists_milestones_MilestoneId",
                table: "task_lists");

            migrationBuilder.DropForeignKey(
                name: "FK_task_lists_projects_ProjectId",
                table: "task_lists");

            migrationBuilder.DropForeignKey(
                name: "FK_task_owners_tasks_TaskId",
                table: "task_owners");

            migrationBuilder.DropForeignKey(
                name: "FK_task_owners_users_OwnersId",
                table: "task_owners");

            migrationBuilder.DropForeignKey(
                name: "FK_tasks_master_lookups_PreviousStatusId",
                table: "tasks");

            migrationBuilder.DropForeignKey(
                name: "FK_tasks_master_lookups_PriorityId",
                table: "tasks");

            migrationBuilder.DropForeignKey(
                name: "FK_tasks_master_lookups_StatusId",
                table: "tasks");

            migrationBuilder.DropForeignKey(
                name: "FK_tasks_milestones_MilestoneId",
                table: "tasks");

            migrationBuilder.DropForeignKey(
                name: "FK_tasks_projects_ProjectId",
                table: "tasks");

            migrationBuilder.DropForeignKey(
                name: "FK_tasks_task_lists_TaskListId",
                table: "tasks");

            migrationBuilder.DropForeignKey(
                name: "FK_tasks_teams_AssociatedTeamId",
                table: "tasks");

            migrationBuilder.DropForeignKey(
                name: "FK_tasks_users_AssigneeId",
                table: "tasks");

            migrationBuilder.DropForeignKey(
                name: "FK_tasks_users_CreatedById",
                table: "tasks");

            migrationBuilder.DropForeignKey(
                name: "FK_tasks_users_OwnerId",
                table: "tasks");

            migrationBuilder.DropForeignKey(
                name: "FK_teams_users_LeadEmail",
                table: "teams");

            migrationBuilder.DropForeignKey(
                name: "FK_timelogs_issues_IssueId",
                table: "timelogs");

            migrationBuilder.DropForeignKey(
                name: "FK_timelogs_master_lookups_ApprovalStatusId",
                table: "timelogs");

            migrationBuilder.DropForeignKey(
                name: "FK_timelogs_master_lookups_PreviousApprovalStatusId",
                table: "timelogs");

            migrationBuilder.DropForeignKey(
                name: "FK_timelogs_projects_ProjectId",
                table: "timelogs");

            migrationBuilder.DropForeignKey(
                name: "FK_timelogs_tasks_TaskId",
                table: "timelogs");

            migrationBuilder.DropForeignKey(
                name: "FK_timelogs_timesheets_TimesheetId",
                table: "timelogs");

            migrationBuilder.DropForeignKey(
                name: "FK_timelogs_users_CreatedById",
                table: "timelogs");

            migrationBuilder.DropForeignKey(
                name: "FK_timelogs_users_UserId",
                table: "timelogs");

            migrationBuilder.DropForeignKey(
                name: "FK_timesheets_projects_ProjectId",
                table: "timesheets");

            migrationBuilder.DropForeignKey(
                name: "FK_timesheets_users_UserEmail",
                table: "timesheets");

            migrationBuilder.DropForeignKey(
                name: "FK_user_skill_link_skills_SkillsId",
                table: "user_skill_link");

            migrationBuilder.DropForeignKey(
                name: "FK_user_skill_link_users_UsersId",
                table: "user_skill_link");

            migrationBuilder.DropForeignKey(
                name: "FK_user_team_link_teams_TeamsId",
                table: "user_team_link");

            migrationBuilder.DropForeignKey(
                name: "FK_user_team_link_users_MembersId",
                table: "user_team_link");

            migrationBuilder.DropForeignKey(
                name: "FK_users_roles_RoleId",
                table: "users");

            migrationBuilder.DropForeignKey(
                name: "FK_users_user_statuses_StatusId",
                table: "users");

            migrationBuilder.DropForeignKey(
                name: "FK_users_users_ManagerEmail",
                table: "users");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_users_Email",
                table: "users");

            migrationBuilder.DropPrimaryKey(
                name: "PK_users",
                table: "users");

            migrationBuilder.DropPrimaryKey(
                name: "PK_timesheets",
                table: "timesheets");

            migrationBuilder.DropPrimaryKey(
                name: "PK_timelogs",
                table: "timelogs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_teams",
                table: "teams");

            migrationBuilder.DropPrimaryKey(
                name: "PK_tasks",
                table: "tasks");

            migrationBuilder.DropPrimaryKey(
                name: "PK_skills",
                table: "skills");

            migrationBuilder.DropPrimaryKey(
                name: "PK_roles",
                table: "roles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_projects",
                table: "projects");

            migrationBuilder.DropPrimaryKey(
                name: "PK_milestones",
                table: "milestones");

            migrationBuilder.DropPrimaryKey(
                name: "PK_issues",
                table: "issues");

            migrationBuilder.DropPrimaryKey(
                name: "PK_documents",
                table: "documents");

            migrationBuilder.DropPrimaryKey(
                name: "PK_user_statuses",
                table: "user_statuses");

            migrationBuilder.DropPrimaryKey(
                name: "PK_task_lists",
                table: "task_lists");

            migrationBuilder.DropPrimaryKey(
                name: "PK_project_members",
                table: "project_members");

            migrationBuilder.DropPrimaryKey(
                name: "PK_project_groups",
                table: "project_groups");

            migrationBuilder.DropPrimaryKey(
                name: "PK_master_lookups",
                table: "master_lookups");

            migrationBuilder.RenameTable(
                name: "users",
                newName: "Users");

            migrationBuilder.RenameTable(
                name: "timesheets",
                newName: "Timesheets");

            migrationBuilder.RenameTable(
                name: "timelogs",
                newName: "Timelogs");

            migrationBuilder.RenameTable(
                name: "teams",
                newName: "Teams");

            migrationBuilder.RenameTable(
                name: "tasks",
                newName: "Tasks");

            migrationBuilder.RenameTable(
                name: "skills",
                newName: "Skills");

            migrationBuilder.RenameTable(
                name: "roles",
                newName: "Roles");

            migrationBuilder.RenameTable(
                name: "projects",
                newName: "Projects");

            migrationBuilder.RenameTable(
                name: "milestones",
                newName: "Milestones");

            migrationBuilder.RenameTable(
                name: "issues",
                newName: "Issues");

            migrationBuilder.RenameTable(
                name: "documents",
                newName: "Documents");

            migrationBuilder.RenameTable(
                name: "user_statuses",
                newName: "UserStatuses");

            migrationBuilder.RenameTable(
                name: "task_lists",
                newName: "TaskLists");

            migrationBuilder.RenameTable(
                name: "project_members",
                newName: "ProjectMembers");

            migrationBuilder.RenameTable(
                name: "project_groups",
                newName: "ProjectGroups");

            migrationBuilder.RenameTable(
                name: "master_lookups",
                newName: "MasterLookups");

            migrationBuilder.RenameIndex(
                name: "IX_users_Username",
                table: "Users",
                newName: "IX_Users_Username");

            migrationBuilder.RenameIndex(
                name: "IX_users_StatusId",
                table: "Users",
                newName: "IX_Users_StatusId");

            migrationBuilder.RenameIndex(
                name: "IX_users_RoleId",
                table: "Users",
                newName: "IX_Users_RoleId");

            migrationBuilder.RenameIndex(
                name: "IX_users_PublicId",
                table: "Users",
                newName: "IX_Users_PublicId");

            migrationBuilder.RenameIndex(
                name: "IX_users_O365Id",
                table: "Users",
                newName: "IX_Users_O365Id");

            migrationBuilder.RenameIndex(
                name: "IX_users_ManagerEmail",
                table: "Users",
                newName: "IX_Users_ManagerEmail");

            migrationBuilder.RenameIndex(
                name: "IX_users_EmployeeId",
                table: "Users",
                newName: "IX_Users_EmployeeId");

            migrationBuilder.RenameIndex(
                name: "IX_users_Email",
                table: "Users",
                newName: "IX_Users_Email");

            migrationBuilder.RenameIndex(
                name: "IX_timesheets_UserEmail",
                table: "Timesheets",
                newName: "IX_Timesheets_UserEmail");

            migrationBuilder.RenameIndex(
                name: "IX_timesheets_ProjectId",
                table: "Timesheets",
                newName: "IX_Timesheets_ProjectId");

            migrationBuilder.RenameIndex(
                name: "IX_timelogs_UserId",
                table: "Timelogs",
                newName: "IX_Timelogs_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_timelogs_TimesheetId",
                table: "Timelogs",
                newName: "IX_Timelogs_TimesheetId");

            migrationBuilder.RenameIndex(
                name: "IX_timelogs_TaskId",
                table: "Timelogs",
                newName: "IX_Timelogs_TaskId");

            migrationBuilder.RenameIndex(
                name: "IX_timelogs_ProjectId",
                table: "Timelogs",
                newName: "IX_Timelogs_ProjectId");

            migrationBuilder.RenameIndex(
                name: "IX_timelogs_PreviousApprovalStatusId",
                table: "Timelogs",
                newName: "IX_Timelogs_PreviousApprovalStatusId");

            migrationBuilder.RenameIndex(
                name: "IX_timelogs_IssueId",
                table: "Timelogs",
                newName: "IX_Timelogs_IssueId");

            migrationBuilder.RenameIndex(
                name: "IX_timelogs_CreatedById",
                table: "Timelogs",
                newName: "IX_Timelogs_CreatedById");

            migrationBuilder.RenameIndex(
                name: "IX_timelogs_ApprovalStatusId",
                table: "Timelogs",
                newName: "IX_Timelogs_ApprovalStatusId");

            migrationBuilder.RenameIndex(
                name: "IX_teams_TeamEmail",
                table: "Teams",
                newName: "IX_Teams_TeamEmail");

            migrationBuilder.RenameIndex(
                name: "IX_teams_PublicId",
                table: "Teams",
                newName: "IX_Teams_PublicId");

            migrationBuilder.RenameIndex(
                name: "IX_teams_LeadEmail",
                table: "Teams",
                newName: "IX_Teams_LeadEmail");

            migrationBuilder.RenameIndex(
                name: "IX_tasks_TaskListId",
                table: "Tasks",
                newName: "IX_Tasks_TaskListId");

            migrationBuilder.RenameIndex(
                name: "IX_tasks_StatusId",
                table: "Tasks",
                newName: "IX_Tasks_StatusId");

            migrationBuilder.RenameIndex(
                name: "IX_tasks_PublicId",
                table: "Tasks",
                newName: "IX_Tasks_PublicId");

            migrationBuilder.RenameIndex(
                name: "IX_tasks_ProjectId",
                table: "Tasks",
                newName: "IX_Tasks_ProjectId");

            migrationBuilder.RenameIndex(
                name: "IX_tasks_PriorityId",
                table: "Tasks",
                newName: "IX_Tasks_PriorityId");

            migrationBuilder.RenameIndex(
                name: "IX_tasks_PreviousStatusId",
                table: "Tasks",
                newName: "IX_Tasks_PreviousStatusId");

            migrationBuilder.RenameIndex(
                name: "IX_tasks_OwnerId",
                table: "Tasks",
                newName: "IX_Tasks_OwnerId");

            migrationBuilder.RenameIndex(
                name: "IX_tasks_MilestoneId",
                table: "Tasks",
                newName: "IX_Tasks_MilestoneId");

            migrationBuilder.RenameIndex(
                name: "IX_tasks_CreatedById",
                table: "Tasks",
                newName: "IX_Tasks_CreatedById");

            migrationBuilder.RenameIndex(
                name: "IX_tasks_AssociatedTeamId",
                table: "Tasks",
                newName: "IX_Tasks_AssociatedTeamId");

            migrationBuilder.RenameIndex(
                name: "IX_tasks_AssigneeId",
                table: "Tasks",
                newName: "IX_Tasks_AssigneeId");

            migrationBuilder.RenameIndex(
                name: "IX_skills_Name",
                table: "Skills",
                newName: "IX_Skills_Name");

            migrationBuilder.RenameIndex(
                name: "IX_roles_Name",
                table: "Roles",
                newName: "IX_Roles_Name");

            migrationBuilder.RenameIndex(
                name: "IX_projects_StatusId",
                table: "Projects",
                newName: "IX_Projects_StatusId");

            migrationBuilder.RenameIndex(
                name: "IX_projects_PublicId",
                table: "Projects",
                newName: "IX_Projects_PublicId");

            migrationBuilder.RenameIndex(
                name: "IX_projects_ProjectName",
                table: "Projects",
                newName: "IX_Projects_ProjectName");

            migrationBuilder.RenameIndex(
                name: "IX_projects_ProjectManagerId",
                table: "Projects",
                newName: "IX_Projects_ProjectManagerId");

            migrationBuilder.RenameIndex(
                name: "IX_projects_ProjectIdSync",
                table: "Projects",
                newName: "IX_Projects_ProjectIdSync");

            migrationBuilder.RenameIndex(
                name: "IX_projects_PriorityId",
                table: "Projects",
                newName: "IX_Projects_PriorityId");

            migrationBuilder.RenameIndex(
                name: "IX_projects_PreviousStatusId",
                table: "Projects",
                newName: "IX_Projects_PreviousStatusId");

            migrationBuilder.RenameIndex(
                name: "IX_projects_OwnerId",
                table: "Projects",
                newName: "IX_Projects_OwnerId");

            migrationBuilder.RenameIndex(
                name: "IX_projects_DeliveryHeadId",
                table: "Projects",
                newName: "IX_Projects_DeliveryHeadId");

            migrationBuilder.RenameIndex(
                name: "IX_milestones_StatusId",
                table: "Milestones",
                newName: "IX_Milestones_StatusId");

            migrationBuilder.RenameIndex(
                name: "IX_milestones_PublicId",
                table: "Milestones",
                newName: "IX_Milestones_PublicId");

            migrationBuilder.RenameIndex(
                name: "IX_milestones_ProjectId",
                table: "Milestones",
                newName: "IX_Milestones_ProjectId");

            migrationBuilder.RenameIndex(
                name: "IX_milestones_PriorityId",
                table: "Milestones",
                newName: "IX_Milestones_PriorityId");

            migrationBuilder.RenameIndex(
                name: "IX_milestones_PreviousStatusId",
                table: "Milestones",
                newName: "IX_Milestones_PreviousStatusId");

            migrationBuilder.RenameIndex(
                name: "IX_milestones_OwnerId",
                table: "Milestones",
                newName: "IX_Milestones_OwnerId");

            migrationBuilder.RenameIndex(
                name: "IX_issues_StatusId",
                table: "Issues",
                newName: "IX_Issues_StatusId");

            migrationBuilder.RenameIndex(
                name: "IX_issues_SeverityId",
                table: "Issues",
                newName: "IX_Issues_SeverityId");

            migrationBuilder.RenameIndex(
                name: "IX_issues_ReporterId",
                table: "Issues",
                newName: "IX_Issues_ReporterId");

            migrationBuilder.RenameIndex(
                name: "IX_issues_PublicId",
                table: "Issues",
                newName: "IX_Issues_PublicId");

            migrationBuilder.RenameIndex(
                name: "IX_issues_ProjectId",
                table: "Issues",
                newName: "IX_Issues_ProjectId");

            migrationBuilder.RenameIndex(
                name: "IX_issues_PriorityId",
                table: "Issues",
                newName: "IX_Issues_PriorityId");

            migrationBuilder.RenameIndex(
                name: "IX_issues_PreviousStatusId",
                table: "Issues",
                newName: "IX_Issues_PreviousStatusId");

            migrationBuilder.RenameIndex(
                name: "IX_issues_MilestoneId",
                table: "Issues",
                newName: "IX_Issues_MilestoneId");

            migrationBuilder.RenameIndex(
                name: "IX_issues_ClassificationId",
                table: "Issues",
                newName: "IX_Issues_ClassificationId");

            migrationBuilder.RenameIndex(
                name: "IX_issues_AssociatedTeamId",
                table: "Issues",
                newName: "IX_Issues_AssociatedTeamId");

            migrationBuilder.RenameIndex(
                name: "IX_issues_AssigneeId",
                table: "Issues",
                newName: "IX_Issues_AssigneeId");

            migrationBuilder.RenameIndex(
                name: "IX_documents_UploadedByEmail",
                table: "Documents",
                newName: "IX_Documents_UploadedByEmail");

            migrationBuilder.RenameIndex(
                name: "IX_documents_ProjectId",
                table: "Documents",
                newName: "IX_Documents_ProjectId");

            migrationBuilder.RenameIndex(
                name: "IX_user_statuses_Name",
                table: "UserStatuses",
                newName: "IX_UserStatuses_Name");

            migrationBuilder.RenameIndex(
                name: "IX_task_lists_ProjectId",
                table: "TaskLists",
                newName: "IX_TaskLists_ProjectId");

            migrationBuilder.RenameIndex(
                name: "IX_task_lists_MilestoneId",
                table: "TaskLists",
                newName: "IX_TaskLists_MilestoneId");

            migrationBuilder.RenameIndex(
                name: "IX_project_members_UserId",
                table: "ProjectMembers",
                newName: "IX_ProjectMembers_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_project_members_PreviousInvitationStatusId",
                table: "ProjectMembers",
                newName: "IX_ProjectMembers_PreviousInvitationStatusId");

            migrationBuilder.RenameIndex(
                name: "IX_project_members_InvitationStatusId",
                table: "ProjectMembers",
                newName: "IX_ProjectMembers_InvitationStatusId");

            migrationBuilder.RenameIndex(
                name: "IX_project_groups_Name",
                table: "ProjectGroups",
                newName: "IX_ProjectGroups_Name");

            migrationBuilder.RenameIndex(
                name: "IX_master_lookups_Category_Value",
                table: "MasterLookups",
                newName: "IX_MasterLookups_Category_Value");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Users_Email",
                table: "Users",
                column: "Email");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Users",
                table: "Users",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Timesheets",
                table: "Timesheets",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Timelogs",
                table: "Timelogs",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Teams",
                table: "Teams",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Tasks",
                table: "Tasks",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Skills",
                table: "Skills",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Roles",
                table: "Roles",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Projects",
                table: "Projects",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Milestones",
                table: "Milestones",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Issues",
                table: "Issues",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Documents",
                table: "Documents",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserStatuses",
                table: "UserStatuses",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TaskLists",
                table: "TaskLists",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProjectMembers",
                table: "ProjectMembers",
                columns: new[] { "ProjectId", "UserId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProjectGroups",
                table: "ProjectGroups",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MasterLookups",
                table: "MasterLookups",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Documents_Projects_ProjectId",
                table: "Documents",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Documents_Users_UploadedByEmail",
                table: "Documents",
                column: "UploadedByEmail",
                principalTable: "Users",
                principalColumn: "Email");

            migrationBuilder.AddForeignKey(
                name: "FK_issue_assignees_Issues_Issue1Id",
                table: "issue_assignees",
                column: "Issue1Id",
                principalTable: "Issues",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_issue_assignees_Users_AssigneesId",
                table: "issue_assignees",
                column: "AssigneesId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_issue_document_link_Documents_DocumentsId",
                table: "issue_document_link",
                column: "DocumentsId",
                principalTable: "Documents",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_issue_document_link_Issues_IssuesId",
                table: "issue_document_link",
                column: "IssuesId",
                principalTable: "Issues",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_issue_followers_Issues_IssueId",
                table: "issue_followers",
                column: "IssueId",
                principalTable: "Issues",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_issue_followers_Users_FollowersId",
                table: "issue_followers",
                column: "FollowersId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Issues_MasterLookups_ClassificationId",
                table: "Issues",
                column: "ClassificationId",
                principalTable: "MasterLookups",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Issues_MasterLookups_PreviousStatusId",
                table: "Issues",
                column: "PreviousStatusId",
                principalTable: "MasterLookups",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Issues_MasterLookups_PriorityId",
                table: "Issues",
                column: "PriorityId",
                principalTable: "MasterLookups",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Issues_MasterLookups_SeverityId",
                table: "Issues",
                column: "SeverityId",
                principalTable: "MasterLookups",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Issues_MasterLookups_StatusId",
                table: "Issues",
                column: "StatusId",
                principalTable: "MasterLookups",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Issues_Milestones_MilestoneId",
                table: "Issues",
                column: "MilestoneId",
                principalTable: "Milestones",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Issues_Projects_ProjectId",
                table: "Issues",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Issues_Teams_AssociatedTeamId",
                table: "Issues",
                column: "AssociatedTeamId",
                principalTable: "Teams",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Issues_Users_AssigneeId",
                table: "Issues",
                column: "AssigneeId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Issues_Users_ReporterId",
                table: "Issues",
                column: "ReporterId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Milestones_MasterLookups_PreviousStatusId",
                table: "Milestones",
                column: "PreviousStatusId",
                principalTable: "MasterLookups",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Milestones_MasterLookups_PriorityId",
                table: "Milestones",
                column: "PriorityId",
                principalTable: "MasterLookups",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Milestones_MasterLookups_StatusId",
                table: "Milestones",
                column: "StatusId",
                principalTable: "MasterLookups",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Milestones_Projects_ProjectId",
                table: "Milestones",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Milestones_Users_OwnerId",
                table: "Milestones",
                column: "OwnerId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectMembers_MasterLookups_InvitationStatusId",
                table: "ProjectMembers",
                column: "InvitationStatusId",
                principalTable: "MasterLookups",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectMembers_MasterLookups_PreviousInvitationStatusId",
                table: "ProjectMembers",
                column: "PreviousInvitationStatusId",
                principalTable: "MasterLookups",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectMembers_Projects_ProjectId",
                table: "ProjectMembers",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectMembers_Users_UserId",
                table: "ProjectMembers",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_MasterLookups_PreviousStatusId",
                table: "Projects",
                column: "PreviousStatusId",
                principalTable: "MasterLookups",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_MasterLookups_PriorityId",
                table: "Projects",
                column: "PriorityId",
                principalTable: "MasterLookups",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_MasterLookups_StatusId",
                table: "Projects",
                column: "StatusId",
                principalTable: "MasterLookups",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_Users_DeliveryHeadId",
                table: "Projects",
                column: "DeliveryHeadId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_Users_OwnerId",
                table: "Projects",
                column: "OwnerId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_Users_ProjectManagerId",
                table: "Projects",
                column: "ProjectManagerId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_task_assignees_Tasks_Task1Id",
                table: "task_assignees",
                column: "Task1Id",
                principalTable: "Tasks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_task_assignees_Users_AssigneesId",
                table: "task_assignees",
                column: "AssigneesId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_task_owners_Tasks_TaskId",
                table: "task_owners",
                column: "TaskId",
                principalTable: "Tasks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_task_owners_Users_OwnersId",
                table: "task_owners",
                column: "OwnersId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TaskLists_Milestones_MilestoneId",
                table: "TaskLists",
                column: "MilestoneId",
                principalTable: "Milestones",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TaskLists_Projects_ProjectId",
                table: "TaskLists",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Tasks_MasterLookups_PreviousStatusId",
                table: "Tasks",
                column: "PreviousStatusId",
                principalTable: "MasterLookups",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Tasks_MasterLookups_PriorityId",
                table: "Tasks",
                column: "PriorityId",
                principalTable: "MasterLookups",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Tasks_MasterLookups_StatusId",
                table: "Tasks",
                column: "StatusId",
                principalTable: "MasterLookups",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Tasks_Milestones_MilestoneId",
                table: "Tasks",
                column: "MilestoneId",
                principalTable: "Milestones",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Tasks_Projects_ProjectId",
                table: "Tasks",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Tasks_TaskLists_TaskListId",
                table: "Tasks",
                column: "TaskListId",
                principalTable: "TaskLists",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Tasks_Teams_AssociatedTeamId",
                table: "Tasks",
                column: "AssociatedTeamId",
                principalTable: "Teams",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Tasks_Users_AssigneeId",
                table: "Tasks",
                column: "AssigneeId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Tasks_Users_CreatedById",
                table: "Tasks",
                column: "CreatedById",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Tasks_Users_OwnerId",
                table: "Tasks",
                column: "OwnerId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Teams_Users_LeadEmail",
                table: "Teams",
                column: "LeadEmail",
                principalTable: "Users",
                principalColumn: "Email");

            migrationBuilder.AddForeignKey(
                name: "FK_Timelogs_Issues_IssueId",
                table: "Timelogs",
                column: "IssueId",
                principalTable: "Issues",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Timelogs_MasterLookups_ApprovalStatusId",
                table: "Timelogs",
                column: "ApprovalStatusId",
                principalTable: "MasterLookups",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Timelogs_MasterLookups_PreviousApprovalStatusId",
                table: "Timelogs",
                column: "PreviousApprovalStatusId",
                principalTable: "MasterLookups",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Timelogs_Projects_ProjectId",
                table: "Timelogs",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Timelogs_Tasks_TaskId",
                table: "Timelogs",
                column: "TaskId",
                principalTable: "Tasks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Timelogs_Timesheets_TimesheetId",
                table: "Timelogs",
                column: "TimesheetId",
                principalTable: "Timesheets",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Timelogs_Users_CreatedById",
                table: "Timelogs",
                column: "CreatedById",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Timelogs_Users_UserId",
                table: "Timelogs",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Timesheets_Projects_ProjectId",
                table: "Timesheets",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Timesheets_Users_UserEmail",
                table: "Timesheets",
                column: "UserEmail",
                principalTable: "Users",
                principalColumn: "Email");

            migrationBuilder.AddForeignKey(
                name: "FK_user_skill_link_Skills_SkillsId",
                table: "user_skill_link",
                column: "SkillsId",
                principalTable: "Skills",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_user_skill_link_Users_UsersId",
                table: "user_skill_link",
                column: "UsersId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_user_team_link_Teams_TeamsId",
                table: "user_team_link",
                column: "TeamsId",
                principalTable: "Teams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_user_team_link_Users_MembersId",
                table: "user_team_link",
                column: "MembersId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Roles_RoleId",
                table: "Users",
                column: "RoleId",
                principalTable: "Roles",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_UserStatuses_StatusId",
                table: "Users",
                column: "StatusId",
                principalTable: "UserStatuses",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Users_ManagerEmail",
                table: "Users",
                column: "ManagerEmail",
                principalTable: "Users",
                principalColumn: "Email");
        }
    }
}
