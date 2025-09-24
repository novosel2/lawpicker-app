using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveIdFromDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_LawDocuments",
                table: "LawDocuments");

            migrationBuilder.DropIndex(
                name: "IX_LawDocuments_Celex",
                table: "LawDocuments");

            migrationBuilder.DropIndex(
                name: "IX_LawDocuments_Id",
                table: "LawDocuments");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "LawDocuments");

            migrationBuilder.AddPrimaryKey(
                name: "PK_LawDocuments",
                table: "LawDocuments",
                column: "Celex");

            migrationBuilder.CreateIndex(
                name: "IX_LawDocuments_Celex",
                table: "LawDocuments",
                column: "Celex");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_LawDocuments",
                table: "LawDocuments");

            migrationBuilder.DropIndex(
                name: "IX_LawDocuments_Celex",
                table: "LawDocuments");

            migrationBuilder.AddColumn<Guid>(
                name: "Id",
                table: "LawDocuments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddPrimaryKey(
                name: "PK_LawDocuments",
                table: "LawDocuments",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_LawDocuments_Celex",
                table: "LawDocuments",
                column: "Celex",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LawDocuments_Id",
                table: "LawDocuments",
                column: "Id");
        }
    }
}
