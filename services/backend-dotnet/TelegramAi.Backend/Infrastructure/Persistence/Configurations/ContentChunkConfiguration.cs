using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TelegramAi.Backend.Domain.Content;

namespace TelegramAi.Backend.Infrastructure.Persistence.Configurations;

public sealed class ContentChunkConfiguration : IEntityTypeConfiguration<ContentChunk>
{
    public void Configure(EntityTypeBuilder<ContentChunk> builder)
    {
        builder.ToTable("content_chunks");

        builder.HasKey(chunk => chunk.Id);

        builder.Property(chunk => chunk.Id)
            .ValueGeneratedNever();

        builder.Property(chunk => chunk.ContentItemId)
            .IsRequired();

        builder.Property(chunk => chunk.Index)
            .IsRequired();

        builder.Property(chunk => chunk.Text)
            .HasColumnType("text")
            .IsRequired();

        builder.Property(chunk => chunk.CharStart)
            .IsRequired();

        builder.Property(chunk => chunk.CharEnd)
            .IsRequired();

        builder.Property(chunk => chunk.CreatedAtUtc)
            .IsRequired();

        builder.HasIndex(chunk => new { chunk.ContentItemId, chunk.Index })
            .IsUnique();

        builder.HasOne<ContentItem>()
            .WithMany()
            .HasForeignKey(chunk => chunk.ContentItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
