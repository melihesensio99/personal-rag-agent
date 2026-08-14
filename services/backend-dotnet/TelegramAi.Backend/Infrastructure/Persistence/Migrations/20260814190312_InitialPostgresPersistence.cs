using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TelegramAi.Backend.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialPostgresPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "contents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RawText = table.Column<string>(type: "text", nullable: false),
                    summary_title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    summary_short_summary = table.Column<string>(type: "text", nullable: false),
                    summary_key_points = table.Column<string>(type: "jsonb", nullable: false),
                    summary_tags = table.Column<string>(type: "jsonb", nullable: false),
                    summary_language = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    summary_provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contents", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "contents");
        }
    }
}
