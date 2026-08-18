using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Text.Json;
using TelegramAi.Backend.Domain.Content;

namespace TelegramAi.Backend.Infrastructure.Persistence.Configurations;

public sealed class ContentItemConfiguration : IEntityTypeConfiguration<ContentItem>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public void Configure(EntityTypeBuilder<ContentItem> builder)
    {
        builder.ToTable("contents");

        builder.HasKey(content => content.Id);

        builder.Property(content => content.Id)
            .ValueGeneratedNever();

        builder.Property(content => content.SourceType)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(content => content.ContentKind)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(content => content.RawText)
            .HasColumnType("text")
            .IsRequired();

        builder.Property(content => content.CreatedAtUtc)
            .IsRequired();

        builder.OwnsOne(content => content.Summary, summary =>
        {
            summary.Property(value => value.Title)
                .HasColumnName("summary_title")
                .HasMaxLength(200)
                .IsRequired();

            summary.Property(value => value.ShortSummary)
                .HasColumnName("summary_short_summary")
                .HasColumnType("text")
                .IsRequired();

            summary.Property(value => value.Language)
                .HasColumnName("summary_language")
                .HasMaxLength(10)
                .IsRequired();

            summary.Property(value => value.Provider)
                .HasColumnName("summary_provider")
                .HasMaxLength(50)
                .IsRequired();

            summary.Property(value => value.KeyPoints)
                .HasColumnName("summary_key_points")
                .HasColumnType("jsonb")
                .HasConversion(CreateStringListConverter())
                .Metadata.SetValueComparer(CreateStringListComparer());

            summary.Property(value => value.Tags)
                .HasColumnName("summary_tags")
                .HasColumnType("jsonb")
                .HasConversion(CreateStringListConverter())
                .Metadata.SetValueComparer(CreateStringListComparer());
        });
    }

    private static ValueConverter<IReadOnlyList<string>, string> CreateStringListConverter()
    {
        return new ValueConverter<IReadOnlyList<string>, string>(
            value => JsonSerializer.Serialize(value, JsonOptions),
            value => JsonSerializer.Deserialize<List<string>>(value, JsonOptions) ?? new List<string>());
    }

    private static ValueComparer<IReadOnlyList<string>> CreateStringListComparer()
    {
        return new ValueComparer<IReadOnlyList<string>>(
            (left, right) => left != null && right != null && left.SequenceEqual(right),
            value => value.Aggregate(0, (current, item) => HashCode.Combine(current, item.GetHashCode())),
            value => value.ToList());
    }
}
