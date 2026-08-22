using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TelegramAi.Backend.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260818221000_AddContentKindToContents")]
    public partial class AddContentKindToContents : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContentKind",
                table: "contents",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Text");

            migrationBuilder.Sql("""
                UPDATE contents
                SET "ContentKind" = CASE
                    WHEN "SourceType" IN ('YouTube', 'Instagram') THEN 'Video'
                    WHEN "SourceType" = 'Image' THEN 'Image'
                    WHEN "SourceType" = 'Article' AND (
                        "RawText" ILIKE 'https://%youtube.com%'
                        OR "RawText" ILIKE 'https://%youtu.be%'
                        OR "RawText" ILIKE 'https://%dailymotion.com%'
                        OR "RawText" ILIKE 'https://%vimeo.com%'
                        OR "RawText" ILIKE 'https://%tiktok.com%'
                        OR "RawText" ILIKE 'https://%instagram.com/reel/%'
                        OR "RawText" ILIKE 'https://%instagram.com/reels/%'
                        OR "RawText" ILIKE 'https://%twitch.tv/%'
                    ) THEN 'Video'
                    ELSE 'Text'
                END;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContentKind",
                table: "contents");
        }
    }
}
