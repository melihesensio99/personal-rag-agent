using System.Text.Json.Serialization;

namespace TelegramAi.Backend.Infrastructure.AiService.Contracts;

public sealed record AiServiceCreateExtractionRequest(
    [property: JsonPropertyName("content_id")] string ContentId,
    [property: JsonPropertyName("source_type")] string? SourceType,
    [property: JsonPropertyName("url")] string? Url,
    [property: JsonPropertyName("text")] string? Text);
