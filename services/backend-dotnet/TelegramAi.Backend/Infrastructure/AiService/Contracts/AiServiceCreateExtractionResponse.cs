using System.Text.Json.Serialization;

namespace TelegramAi.Backend.Infrastructure.AiService.Contracts;

public sealed record AiServiceCreateExtractionResponse(
    [property: JsonPropertyName("content_id")] string ContentId,
    [property: JsonPropertyName("source_type")] string SourceType,
    [property: JsonPropertyName("extraction_status")] string ExtractionStatus,
    [property: JsonPropertyName("title")] string? Title,
    [property: JsonPropertyName("extracted_text")] string ExtractedText,
    [property: JsonPropertyName("original_url")] string? OriginalUrl,
    [property: JsonPropertyName("metadata")] AiServiceExtractionMetadataResponse Metadata);
