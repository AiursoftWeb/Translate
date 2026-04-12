using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aiursoft.Translate.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class AddTranslationCache : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TranslationCaches",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SourceHash = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    SourceContent = table.Column<string>(type: "TEXT", nullable: false),
                    TargetLanguage = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    TranslatedContent = table.Column<string>(type: "TEXT", nullable: false),
                    CreateTime = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TranslationCaches", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TranslationCaches");
        }
    }
}
