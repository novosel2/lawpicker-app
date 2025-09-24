using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class NewSeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Languages",
                keyColumn: "LanguageCode",
                keyValue: "BUL");

            migrationBuilder.DeleteData(
                table: "Languages",
                keyColumn: "LanguageCode",
                keyValue: "CES");

            migrationBuilder.DeleteData(
                table: "Languages",
                keyColumn: "LanguageCode",
                keyValue: "DAN");

            migrationBuilder.DeleteData(
                table: "Languages",
                keyColumn: "LanguageCode",
                keyValue: "DEU");

            migrationBuilder.DeleteData(
                table: "Languages",
                keyColumn: "LanguageCode",
                keyValue: "ELL");

            migrationBuilder.DeleteData(
                table: "Languages",
                keyColumn: "LanguageCode",
                keyValue: "ENG");

            migrationBuilder.DeleteData(
                table: "Languages",
                keyColumn: "LanguageCode",
                keyValue: "EST");

            migrationBuilder.DeleteData(
                table: "Languages",
                keyColumn: "LanguageCode",
                keyValue: "FIN");

            migrationBuilder.DeleteData(
                table: "Languages",
                keyColumn: "LanguageCode",
                keyValue: "FRA");

            migrationBuilder.DeleteData(
                table: "Languages",
                keyColumn: "LanguageCode",
                keyValue: "GLE");

            migrationBuilder.DeleteData(
                table: "Languages",
                keyColumn: "LanguageCode",
                keyValue: "HRV");

            migrationBuilder.DeleteData(
                table: "Languages",
                keyColumn: "LanguageCode",
                keyValue: "HUN");

            migrationBuilder.DeleteData(
                table: "Languages",
                keyColumn: "LanguageCode",
                keyValue: "ITA");

            migrationBuilder.DeleteData(
                table: "Languages",
                keyColumn: "LanguageCode",
                keyValue: "LAV");

            migrationBuilder.DeleteData(
                table: "Languages",
                keyColumn: "LanguageCode",
                keyValue: "LIT");

            migrationBuilder.DeleteData(
                table: "Languages",
                keyColumn: "LanguageCode",
                keyValue: "MLT");

            migrationBuilder.DeleteData(
                table: "Languages",
                keyColumn: "LanguageCode",
                keyValue: "NLD");

            migrationBuilder.DeleteData(
                table: "Languages",
                keyColumn: "LanguageCode",
                keyValue: "POL");

            migrationBuilder.DeleteData(
                table: "Languages",
                keyColumn: "LanguageCode",
                keyValue: "POR");

            migrationBuilder.DeleteData(
                table: "Languages",
                keyColumn: "LanguageCode",
                keyValue: "RON");

            migrationBuilder.DeleteData(
                table: "Languages",
                keyColumn: "LanguageCode",
                keyValue: "SLK");

            migrationBuilder.DeleteData(
                table: "Languages",
                keyColumn: "LanguageCode",
                keyValue: "SLV");

            migrationBuilder.DeleteData(
                table: "Languages",
                keyColumn: "LanguageCode",
                keyValue: "SPA");

            migrationBuilder.DeleteData(
                table: "Languages",
                keyColumn: "LanguageCode",
                keyValue: "SWE");

            migrationBuilder.InsertData(
                table: "Languages",
                columns: new[] { "LanguageCode", "LanguageName" },
                values: new object[,]
                {
                    { "BG", "Bulgarian" },
                    { "CS", "Czech" },
                    { "DA", "Danish" },
                    { "DE", "German" },
                    { "EL", "Greek" },
                    { "EN", "English" },
                    { "ES", "Spanish" },
                    { "ET", "Estonian" },
                    { "FI", "Finnish" },
                    { "FR", "French" },
                    { "GA", "Irish" },
                    { "HR", "Croatian" },
                    { "HU", "Hungarian" },
                    { "IT", "Italian" },
                    { "LT", "Lithuanian" },
                    { "LV", "Latvian" },
                    { "MT", "Maltese" },
                    { "NL", "Dutch" },
                    { "PL", "Polish" },
                    { "PT", "Portuguese" },
                    { "RO", "Romanian" },
                    { "SK", "Slovak" },
                    { "SL", "Slovenian" },
                    { "SV", "Swedish" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Languages",
                keyColumn: "LanguageCode",
                keyValue: "BG");

            migrationBuilder.DeleteData(
                table: "Languages",
                keyColumn: "LanguageCode",
                keyValue: "CS");

            migrationBuilder.DeleteData(
                table: "Languages",
                keyColumn: "LanguageCode",
                keyValue: "DA");

            migrationBuilder.DeleteData(
                table: "Languages",
                keyColumn: "LanguageCode",
                keyValue: "DE");

            migrationBuilder.DeleteData(
                table: "Languages",
                keyColumn: "LanguageCode",
                keyValue: "EL");

            migrationBuilder.DeleteData(
                table: "Languages",
                keyColumn: "LanguageCode",
                keyValue: "EN");

            migrationBuilder.DeleteData(
                table: "Languages",
                keyColumn: "LanguageCode",
                keyValue: "ES");

            migrationBuilder.DeleteData(
                table: "Languages",
                keyColumn: "LanguageCode",
                keyValue: "ET");

            migrationBuilder.DeleteData(
                table: "Languages",
                keyColumn: "LanguageCode",
                keyValue: "FI");

            migrationBuilder.DeleteData(
                table: "Languages",
                keyColumn: "LanguageCode",
                keyValue: "FR");

            migrationBuilder.DeleteData(
                table: "Languages",
                keyColumn: "LanguageCode",
                keyValue: "GA");

            migrationBuilder.DeleteData(
                table: "Languages",
                keyColumn: "LanguageCode",
                keyValue: "HR");

            migrationBuilder.DeleteData(
                table: "Languages",
                keyColumn: "LanguageCode",
                keyValue: "HU");

            migrationBuilder.DeleteData(
                table: "Languages",
                keyColumn: "LanguageCode",
                keyValue: "IT");

            migrationBuilder.DeleteData(
                table: "Languages",
                keyColumn: "LanguageCode",
                keyValue: "LT");

            migrationBuilder.DeleteData(
                table: "Languages",
                keyColumn: "LanguageCode",
                keyValue: "LV");

            migrationBuilder.DeleteData(
                table: "Languages",
                keyColumn: "LanguageCode",
                keyValue: "MT");

            migrationBuilder.DeleteData(
                table: "Languages",
                keyColumn: "LanguageCode",
                keyValue: "NL");

            migrationBuilder.DeleteData(
                table: "Languages",
                keyColumn: "LanguageCode",
                keyValue: "PL");

            migrationBuilder.DeleteData(
                table: "Languages",
                keyColumn: "LanguageCode",
                keyValue: "PT");

            migrationBuilder.DeleteData(
                table: "Languages",
                keyColumn: "LanguageCode",
                keyValue: "RO");

            migrationBuilder.DeleteData(
                table: "Languages",
                keyColumn: "LanguageCode",
                keyValue: "SK");

            migrationBuilder.DeleteData(
                table: "Languages",
                keyColumn: "LanguageCode",
                keyValue: "SL");

            migrationBuilder.DeleteData(
                table: "Languages",
                keyColumn: "LanguageCode",
                keyValue: "SV");

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
        }
    }
}
