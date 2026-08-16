using System.Text.Json.Serialization;

namespace TelegramAi.Backend.Infrastructure.AiService.Contracts;

public sealed record AiServiceClassifyIntentResponse(
    [property: JsonPropertyName("intent")] string Intent,
    [property: JsonPropertyName("source_type")] string? SourceType,
    [property: JsonPropertyName("time_filter")] string TimeFilter,
    [property: JsonPropertyName("keywords")] IReadOnlyList<string> Keywords,
    [property: JsonPropertyName("needs_clarification")] bool NeedsClarification);
