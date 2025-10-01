using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ChangeNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DocumentType",
                table: "LawDocuments",
                newName: "Type");

            migrationBuilder.RenameColumn(
                name: "DocumentTitle",
                table: "LawDocuments",
                newName: "Title");

            migrationBuilder.RenameColumn(
                name: "DocumentDate",
                table: "LawDocuments",
                newName: "Date");

            migrationBuilder.RenameColumn(
                name: "DocumentCELEX",
                table: "LawDocuments",
                newName: "Celex");

            migrationBuilder.RenameIndex(
                name: "IX_LawDocuments_DocumentCELEX",
                table: "LawDocuments",
                newName: "IX_LawDocuments_Celex");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Type",
                table: "LawDocuments",
                newName: "DocumentType");

            migrationBuilder.RenameColumn(
                name: "Title",
                table: "LawDocuments",
                newName: "DocumentTitle");

            migrationBuilder.RenameColumn(
                name: "Date",
                table: "LawDocuments",
                newName: "DocumentDate");

            migrationBuilder.RenameColumn(
                name: "Celex",
                table: "LawDocuments",
                newName: "DocumentCELEX");

            migrationBuilder.RenameIndex(
                name: "IX_LawDocuments_Celex",
                table: "LawDocuments",
                newName: "IX_LawDocuments_DocumentCELEX");
        }
    }
}
