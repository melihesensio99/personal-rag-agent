using Microsoft.EntityFrameworkCore.Migrations;
using Pgvector;

#nullable disable

namespace TelegramAi.Backend.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddContentChunkEmbeddings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS vector;");

            migrationBuilder.AddColumn<Vector>(
                name: "Embedding",
                table: "content_chunks",
                type: "vector(1024)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Embedding",
                table: "content_chunks");
        }
    }
}
