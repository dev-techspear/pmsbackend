using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTemplatesAndAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditFieldsMappings",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EntityName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FieldName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsVisible = table.Column<bool>(type: "bit", nullable: true),
                    OrderNo = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditFieldsMappings", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TableName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Action = table.Column<int>(type: "int", nullable: false),
                    PerformedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PerformedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RecordName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TransactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Comments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModuleName = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "AuditMetaDataInfos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    File_name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModuleOrEntityName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditMetaDataInfos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "project_templates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BillingType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsPublic = table.Column<bool>(type: "bit", nullable: false),
                    SourceProjectId = table.Column<int>(type: "int", nullable: true),
                    CreatedById = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_project_templates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_project_templates_projects_SourceProjectId",
                        column: x => x.SourceProjectId,
                        principalTable: "projects",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_project_templates_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AuditLogDetails",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AuditLogId = table.Column<long>(type: "bigint", nullable: false),
                    FieldName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OldValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ValueType = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditLogDetails_AuditLogs_AuditLogId",
                        column: x => x.AuditLogId,
                        principalTable: "AuditLogs",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "template_tasks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TemplateId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EstimatedHours = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                    Duration = table.Column<int>(type: "int", nullable: true),
                    BillingType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Tags = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OrderIndex = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_template_tasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_template_tasks_project_templates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "project_templates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_projects_TemplateId",
                table: "projects",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogDetails_AuditLogId",
                table: "AuditLogDetails",
                column: "AuditLogId");

            migrationBuilder.CreateIndex(
                name: "IX_project_templates_CreatedById",
                table: "project_templates",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_project_templates_Name",
                table: "project_templates",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_project_templates_SourceProjectId",
                table: "project_templates",
                column: "SourceProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_template_tasks_TemplateId_Title",
                table: "template_tasks",
                columns: new[] { "TemplateId", "Title" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_projects_project_templates_TemplateId",
                table: "projects",
                column: "TemplateId",
                principalTable: "project_templates",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_projects_project_templates_TemplateId",
                table: "projects");

            migrationBuilder.DropTable(
                name: "AuditFieldsMappings");

            migrationBuilder.DropTable(
                name: "AuditLogDetails");

            migrationBuilder.DropTable(
                name: "AuditMetaDataInfos");

            migrationBuilder.DropTable(
                name: "template_tasks");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "project_templates");

            migrationBuilder.DropIndex(
                name: "IX_projects_TemplateId",
                table: "projects");
        }
    }
}
