using Microsoft.EntityFrameworkCore;
using TelegramAi.Backend.Domain.Content;
using TelegramAi.Backend.Infrastructure.Persistence.Configurations;

namespace TelegramAi.Backend.Infrastructure.Persistence;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<ContentItem> Contents => Set<ContentItem>();
    public DbSet<ContentChunk> ContentChunks => Set<ContentChunk>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("vector");
        modelBuilder.HasPostgresExtension("pg_trgm");

        modelBuilder.ApplyConfiguration(new ContentItemConfiguration());
        modelBuilder.ApplyConfiguration(new ContentChunkConfiguration());
    }
}
