using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSubtasksLinksAndNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ParentTaskId",
                schema: "pms",
                table: "tasks",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "project_links",
                schema: "pms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Url = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    LinkType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RestrictAccess = table.Column<bool>(type: "bit", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_project_links", x => x.Id);
                    table.ForeignKey(
                        name: "FK_project_links_projects_ProjectId",
                        column: x => x.ProjectId,
                        principalSchema: "pms",
                        principalTable: "projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_project_links_users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalSchema: "pms",
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "project_notes",
                schema: "pms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Body = table.Column<string>(type: "nvarchar(max)", maxLength: 10000, nullable: false),
                    IsPinned = table.Column<bool>(type: "bit", nullable: false),
                    RestrictAccess = table.Column<bool>(type: "bit", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_project_notes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_project_notes_projects_ProjectId",
                        column: x => x.ProjectId,
                        principalSchema: "pms",
                        principalTable: "projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_project_notes_users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalSchema: "pms",
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "project_link_role_accesses",
                schema: "pms",
                columns: table => new
                {
                    LinkId = table.Column<int>(type: "int", nullable: false),
                    RoleId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_project_link_role_accesses", x => new { x.LinkId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_project_link_role_accesses_project_links_LinkId",
                        column: x => x.LinkId,
                        principalSchema: "pms",
                        principalTable: "project_links",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_project_link_role_accesses_roles_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "pms",
                        principalTable: "roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "project_link_user_accesses",
                schema: "pms",
                columns: table => new
                {
                    LinkId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_project_link_user_accesses", x => new { x.LinkId, x.UserId });
                    table.ForeignKey(
                        name: "FK_project_link_user_accesses_project_links_LinkId",
                        column: x => x.LinkId,
                        principalSchema: "pms",
                        principalTable: "project_links",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_project_link_user_accesses_users_UserId",
                        column: x => x.UserId,
                        principalSchema: "pms",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "project_note_role_accesses",
                schema: "pms",
                columns: table => new
                {
                    NoteId = table.Column<int>(type: "int", nullable: false),
                    RoleId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_project_note_role_accesses", x => new { x.NoteId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_project_note_role_accesses_project_notes_NoteId",
                        column: x => x.NoteId,
                        principalSchema: "pms",
                        principalTable: "project_notes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_project_note_role_accesses_roles_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "pms",
                        principalTable: "roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "project_note_user_accesses",
                schema: "pms",
                columns: table => new
                {
                    NoteId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_project_note_user_accesses", x => new { x.NoteId, x.UserId });
                    table.ForeignKey(
                        name: "FK_project_note_user_accesses_project_notes_NoteId",
                        column: x => x.NoteId,
                        principalSchema: "pms",
                        principalTable: "project_notes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_project_note_user_accesses_users_UserId",
                        column: x => x.UserId,
                        principalSchema: "pms",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tasks_ParentTaskId",
                schema: "pms",
                table: "tasks",
                column: "ParentTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_project_link_role_accesses_RoleId",
                schema: "pms",
                table: "project_link_role_accesses",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_project_link_user_accesses_UserId",
                schema: "pms",
                table: "project_link_user_accesses",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_project_links_CreatedByUserId",
                schema: "pms",
                table: "project_links",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_project_links_ProjectId",
                schema: "pms",
                table: "project_links",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_project_note_role_accesses_RoleId",
                schema: "pms",
                table: "project_note_role_accesses",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_project_note_user_accesses_UserId",
                schema: "pms",
                table: "project_note_user_accesses",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_project_notes_CreatedByUserId",
                schema: "pms",
                table: "project_notes",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_project_notes_ProjectId",
                schema: "pms",
                table: "project_notes",
                column: "ProjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_tasks_tasks_ParentTaskId",
                schema: "pms",
                table: "tasks",
                column: "ParentTaskId",
                principalSchema: "pms",
                principalTable: "tasks",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_tasks_tasks_ParentTaskId",
                schema: "pms",
                table: "tasks");

            migrationBuilder.DropTable(
                name: "project_link_role_accesses",
                schema: "pms");

            migrationBuilder.DropTable(
                name: "project_link_user_accesses",
                schema: "pms");

            migrationBuilder.DropTable(
                name: "project_note_role_accesses",
                schema: "pms");

            migrationBuilder.DropTable(
                name: "project_note_user_accesses",
                schema: "pms");

            migrationBuilder.DropTable(
                name: "project_links",
                schema: "pms");

            migrationBuilder.DropTable(
                name: "project_notes",
                schema: "pms");

            migrationBuilder.DropIndex(
                name: "IX_tasks_ParentTaskId",
                schema: "pms",
                table: "tasks");

            migrationBuilder.DropColumn(
                name: "ParentTaskId",
                schema: "pms",
                table: "tasks");
        }
    }
}
