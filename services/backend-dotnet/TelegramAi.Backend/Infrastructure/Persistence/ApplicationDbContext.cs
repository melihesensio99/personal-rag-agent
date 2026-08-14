using Microsoft.EntityFrameworkCore;
using TelegramAi.Backend.Domain.Content;
using TelegramAi.Backend.Infrastructure.Persistence.Configurations;

namespace TelegramAi.Backend.Infrastructure.Persistence;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<ContentItem> Contents => Set<ContentItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new ContentItemConfiguration());
    }
}
