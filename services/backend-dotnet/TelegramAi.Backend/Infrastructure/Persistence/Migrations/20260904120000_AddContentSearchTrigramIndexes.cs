using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TelegramAi.Backend.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddContentSearchTrigramIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pg_trgm;");
            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS "IX_contents_RawText_trgm"
                ON contents
                USING gin ("RawText" gin_trgm_ops);
                """);
            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS "IX_contents_summary_title_trgm"
                ON contents
                USING gin (summary_title gin_trgm_ops);
                """);
            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS "IX_contents_summary_short_summary_trgm"
                ON contents
                USING gin (summary_short_summary gin_trgm_ops);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""DROP INDEX IF EXISTS "IX_contents_summary_short_summary_trgm";""");
            migrationBuilder.Sql("""DROP INDEX IF EXISTS "IX_contents_summary_title_trgm";""");
            migrationBuilder.Sql("""DROP INDEX IF EXISTS "IX_contents_RawText_trgm";""");
        }
    }
}
