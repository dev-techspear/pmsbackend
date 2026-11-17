using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentSharingAndCredentials : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BlobPath",
                schema: "pms",
                table: "documents",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RestrictAccess",
                schema: "pms",
                table: "documents",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "document_role_accesses",
                schema: "pms",
                columns: table => new
                {
                    DocumentId = table.Column<int>(type: "int", nullable: false),
                    RoleId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_document_role_accesses", x => new { x.DocumentId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_document_role_accesses_documents_DocumentId",
                        column: x => x.DocumentId,
                        principalSchema: "pms",
                        principalTable: "documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_document_role_accesses_roles_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "pms",
                        principalTable: "roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "document_user_accesses",
                schema: "pms",
                columns: table => new
                {
                    DocumentId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_document_user_accesses", x => new { x.DocumentId, x.UserId });
                    table.ForeignKey(
                        name: "FK_document_user_accesses_documents_DocumentId",
                        column: x => x.DocumentId,
                        principalSchema: "pms",
                        principalTable: "documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_document_user_accesses_users_UserId",
                        column: x => x.UserId,
                        principalSchema: "pms",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "project_credential_access_logs",
                schema: "pms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CredentialId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    At = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_project_credential_access_logs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "project_credentials",
                schema: "pms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    Label = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CredentialType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Host = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Username = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    SecretEncrypted = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    LastRotatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RestrictAccess = table.Column<bool>(type: "bit", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_project_credentials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_project_credentials_projects_ProjectId",
                        column: x => x.ProjectId,
                        principalSchema: "pms",
                        principalTable: "projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_project_credentials_users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalSchema: "pms",
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "project_credential_role_accesses",
                schema: "pms",
                columns: table => new
                {
                    CredentialId = table.Column<int>(type: "int", nullable: false),
                    RoleId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_project_credential_role_accesses", x => new { x.CredentialId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_project_credential_role_accesses_project_credentials_CredentialId",
                        column: x => x.CredentialId,
                        principalSchema: "pms",
                        principalTable: "project_credentials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_project_credential_role_accesses_roles_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "pms",
                        principalTable: "roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "project_credential_user_accesses",
                schema: "pms",
                columns: table => new
                {
                    CredentialId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_project_credential_user_accesses", x => new { x.CredentialId, x.UserId });
                    table.ForeignKey(
                        name: "FK_project_credential_user_accesses_project_credentials_CredentialId",
                        column: x => x.CredentialId,
                        principalSchema: "pms",
                        principalTable: "project_credentials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_project_credential_user_accesses_users_UserId",
                        column: x => x.UserId,
                        principalSchema: "pms",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_document_role_accesses_RoleId",
                schema: "pms",
                table: "document_role_accesses",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_document_user_accesses_UserId",
                schema: "pms",
                table: "document_user_accesses",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_project_credential_access_logs_CredentialId",
                schema: "pms",
                table: "project_credential_access_logs",
                column: "CredentialId");

            migrationBuilder.CreateIndex(
                name: "IX_project_credential_role_accesses_RoleId",
                schema: "pms",
                table: "project_credential_role_accesses",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_project_credential_user_accesses_UserId",
                schema: "pms",
                table: "project_credential_user_accesses",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_project_credentials_CreatedByUserId",
                schema: "pms",
                table: "project_credentials",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_project_credentials_ProjectId",
                schema: "pms",
                table: "project_credentials",
                column: "ProjectId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "document_role_accesses",
                schema: "pms");

            migrationBuilder.DropTable(
                name: "document_user_accesses",
                schema: "pms");

            migrationBuilder.DropTable(
                name: "project_credential_access_logs",
                schema: "pms");

            migrationBuilder.DropTable(
                name: "project_credential_role_accesses",
                schema: "pms");

            migrationBuilder.DropTable(
                name: "project_credential_user_accesses",
                schema: "pms");

            migrationBuilder.DropTable(
                name: "project_credentials",
                schema: "pms");

            migrationBuilder.DropColumn(
                name: "BlobPath",
                schema: "pms",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "RestrictAccess",
                schema: "pms",
                table: "documents");
        }
    }
}
