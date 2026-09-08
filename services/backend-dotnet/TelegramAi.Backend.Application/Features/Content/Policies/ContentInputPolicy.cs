using TelegramAi.Backend.Application.Contracts.Extractions;
using TelegramAi.Backend.Application.Features.Content.Commands;
using TelegramAi.Backend.Domain.Content;

namespace TelegramAi.Backend.Application.Features.Content.Policies;

internal static class ContentInputPolicy
{
    public static string ResolveSummaryInputText(CreateContentCommand command, CreateExtractionResult? extraction)
    {
        if (!string.IsNullOrWhiteSpace(command.SummaryInputText)) return command.SummaryInputText.Trim();
        if (IsCompleted(extraction) && !string.IsNullOrWhiteSpace(extraction!.ExtractedText)) return BuildSummaryInputText(extraction);
        return command.Text.Trim();
    }

    public static string ResolveChunkInputText(CreateContentCommand command, CreateExtractionResult? extraction, string summaryInputText)
    {
        if (IsCompleted(extraction) && !string.IsNullOrWhiteSpace(extraction!.ExtractedText)) return extraction.ExtractedText.Trim();
        if (!string.IsNullOrWhiteSpace(command.SummaryInputText)) return command.SummaryInputText.Trim();
        return summaryInputText.Trim();
    }

    public static ContentKind ResolveContentKind(CreateContentCommand command, CreateExtractionResult? extraction)
        => extraction is not null ? ContentKindMapper.FromDetectedContentKind(extraction.DetectedContentKind, command.SourceType ?? ContentSourceType.Telegram) : ContentKindMapper.FromSourceType(command.SourceType ?? ContentSourceType.Telegram);

    public static ContentSourceType ResolveSourceType(CreateContentCommand command, CreateExtractionResult? extraction)
        => extraction is not null && Enum.TryParse<ContentSourceType>(extraction.SourceType, true, out var parsed) ? parsed : command.SourceType ?? ContentSourceType.Telegram;

    public static string? TryExtractUrl(string text)
    {
        var firstToken = text.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();
        return Uri.TryCreate(firstToken, UriKind.Absolute, out var uri) ? uri.ToString() : null;
    }

    private static bool IsCompleted(CreateExtractionResult? extraction) => extraction is not null && extraction.ExtractionStatus.Equals("completed", StringComparison.OrdinalIgnoreCase);

    private static string BuildSummaryInputText(CreateExtractionResult extraction)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(extraction.Title)) parts.Add($"Title: {extraction.Title.Trim()}");
        if (!string.IsNullOrWhiteSpace(extraction.OriginalUrl)) parts.Add($"Original URL: {extraction.OriginalUrl.Trim()}");
        parts.Add(extraction.ExtractedText.Trim());
        return string.Join(Environment.NewLine, parts);
    }
}
