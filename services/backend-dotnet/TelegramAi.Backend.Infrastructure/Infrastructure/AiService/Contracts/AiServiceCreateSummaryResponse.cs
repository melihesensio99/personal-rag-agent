using System.Text.Json.Serialization;

namespace TelegramAi.Backend.Infrastructure.AiService.Contracts;

public sealed record AiServiceCreateSummaryResponse(
    [property: JsonPropertyName("content_id")] string ContentId,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("short_summary")] string ShortSummary,
    [property: JsonPropertyName("key_points")] IReadOnlyList<string> KeyPoints,
    [property: JsonPropertyName("tags")] IReadOnlyList<string> Tags,
    [property: JsonPropertyName("language")] string Language,
    [property: JsonPropertyName("provider")] string Provider);
