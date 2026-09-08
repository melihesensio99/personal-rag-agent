using System.Text.Json.Serialization;

namespace TelegramAi.Backend.Infrastructure.AiService.Contracts;

public sealed record AiServiceClassifyIntentResponse(
    [property: JsonPropertyName("action")] string Action,
    [property: JsonPropertyName("intent")] string Intent,
    [property: JsonPropertyName("query")] string? Query,
    [property: JsonPropertyName("content")] string? Content,
    [property: JsonPropertyName("content_kind")] string? ContentKind,
    [property: JsonPropertyName("source_type")] string? SourceType,
    [property: JsonPropertyName("time_filter")] string TimeFilter,
    [property: JsonPropertyName("date_from")] string? DateFrom,
    [property: JsonPropertyName("date_to")] string? DateTo,
    [property: JsonPropertyName("semantic_query")] string? SemanticQuery,
    [property: JsonPropertyName("keywords")] IReadOnlyList<string> Keywords,
    [property: JsonPropertyName("needs_clarification")] bool NeedsClarification,
    [property: JsonPropertyName("clarification_message")] string? ClarificationMessage);
