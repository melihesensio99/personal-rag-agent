using System.Text.Json.Serialization;

namespace TelegramAi.Backend.Infrastructure.AiService.Contracts;

public sealed record AiServiceExtractionMetadataResponse(
    [property: JsonPropertyName("domain")] string? Domain,
    [property: JsonPropertyName("content_type")] string? ContentType,
    [property: JsonPropertyName("final_url")] string? FinalUrl,
    [property: JsonPropertyName("extra")] Dictionary<string, object?> Extra);
