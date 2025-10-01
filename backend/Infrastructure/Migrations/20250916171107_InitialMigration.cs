using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LawDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentCELEX = table.Column<string>(type: "text", nullable: false),
                    DocumentType = table.Column<char>(type: "character(1)", nullable: false),
                    DocumentDate = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LawDocuments", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LawDocuments_DocumentCELEX",
                table: "LawDocuments",
                column: "DocumentCELEX",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LawDocuments_Id",
                table: "LawDocuments",
                column: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LawDocuments");
        }
    }
}
