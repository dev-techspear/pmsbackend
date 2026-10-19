using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixAuditTableNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_AuditMetaDataInfos",
                table: "AuditMetaDataInfos");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AuditFieldsMappings",
                table: "AuditFieldsMappings");

            migrationBuilder.RenameTable(
                name: "AuditMetaDataInfos",
                newName: "AuditMetaDataInfo");

            migrationBuilder.RenameTable(
                name: "AuditFieldsMappings",
                newName: "AuditFieldsMapping");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AuditMetaDataInfo",
                table: "AuditMetaDataInfo",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AuditFieldsMapping",
                table: "AuditFieldsMapping",
                column: "ID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_AuditMetaDataInfo",
                table: "AuditMetaDataInfo");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AuditFieldsMapping",
                table: "AuditFieldsMapping");

            migrationBuilder.RenameTable(
                name: "AuditMetaDataInfo",
                newName: "AuditMetaDataInfos");

            migrationBuilder.RenameTable(
                name: "AuditFieldsMapping",
                newName: "AuditFieldsMappings");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AuditMetaDataInfos",
                table: "AuditMetaDataInfos",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AuditFieldsMappings",
                table: "AuditFieldsMappings",
                column: "ID");
        }
    }
}
