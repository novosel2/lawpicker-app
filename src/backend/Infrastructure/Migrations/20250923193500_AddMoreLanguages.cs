using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMoreLanguages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LawDocuments_Celex",
                table: "LawDocuments");

            migrationBuilder.DropColumn(
                name: "Languages",
                table: "LawDocuments");

            migrationBuilder.CreateTable(
                name: "Languages",
                columns: table => new
                {
                    LanguageCode = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    LanguageName = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Languages", x => x.LanguageCode);
                });

            migrationBuilder.CreateTable(
                name: "DocumentLanguages",
                columns: table => new
                {
                    CelexNumber = table.Column<string>(type: "text", nullable: false),
                    LanguageCode = table.Column<string>(type: "character varying(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentLanguages", x => new { x.CelexNumber, x.LanguageCode });
                    table.ForeignKey(
                        name: "FK_DocumentLanguages_Languages_LanguageCode",
                        column: x => x.LanguageCode,
                        principalTable: "Languages",
                        principalColumn: "LanguageCode",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DocumentLanguages_LawDocuments_CelexNumber",
                        column: x => x.CelexNumber,
                        principalTable: "LawDocuments",
                        principalColumn: "Celex",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Languages",
                columns: new[] { "LanguageCode", "LanguageName" },
                values: new object[,]
                {
                    { "BUL", "Bulgarian" },
                    { "CES", "Czech" },
                    { "DAN", "Danish" },
                    { "DEU", "German" },
                    { "ELL", "Greek" },
                    { "ENG", "English" },
                    { "EST", "Estonian" },
                    { "FIN", "Finnish" },
                    { "FRA", "French" },
                    { "GLE", "Irish" },
                    { "HRV", "Croatian" },
                    { "HUN", "Hungarian" },
                    { "ITA", "Italian" },
                    { "LAV", "Latvian" },
                    { "LIT", "Lithuanian" },
                    { "MLT", "Maltese" },
                    { "NLD", "Dutch" },
                    { "POL", "Polish" },
                    { "POR", "Portuguese" },
                    { "RON", "Romanian" },
                    { "SLK", "Slovak" },
                    { "SLV", "Slovenian" },
                    { "SPA", "Spanish" },
                    { "SWE", "Swedish" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_LawDocuments_Celex",
                table: "LawDocuments",
                column: "Celex",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DocumentLanguages_LanguageCode",
                table: "DocumentLanguages",
                column: "LanguageCode");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DocumentLanguages");

            migrationBuilder.DropTable(
                name: "Languages");

            migrationBuilder.DropIndex(
                name: "IX_LawDocuments_Celex",
                table: "LawDocuments");

            migrationBuilder.AddColumn<List<string>>(
                name: "Languages",
                table: "LawDocuments",
                type: "text[]",
                nullable: false);

            migrationBuilder.CreateIndex(
                name: "IX_LawDocuments_Celex",
                table: "LawDocuments",
                column: "Celex");
        }
    }
}
