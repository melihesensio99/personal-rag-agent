using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using TelegramAi.Backend.Infrastructure.Persistence;

#nullable disable

namespace TelegramAi.Backend.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260908232000_AddContentSourceMedia")]
public partial class AddContentSourceMedia : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name: "ImageUrl", table: "contents", type: "character varying(2048)", maxLength: 2048, nullable: true);
        migrationBuilder.AddColumn<string>(name: "OriginalUrl", table: "contents", type: "character varying(2048)", maxLength: 2048, nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "ImageUrl", table: "contents");
        migrationBuilder.DropColumn(name: "OriginalUrl", table: "contents");
    }
}
