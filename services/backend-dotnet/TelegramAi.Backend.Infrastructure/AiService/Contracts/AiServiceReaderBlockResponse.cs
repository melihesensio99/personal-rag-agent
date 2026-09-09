using System.Text.Json.Serialization;

namespace TelegramAi.Backend.Infrastructure.AiService.Contracts;

public sealed record AiServiceReaderBlockResponse(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("text")] string? Text,
    [property: JsonPropertyName("level")] int? Level,
    [property: JsonPropertyName("url")] string? Url,
    [property: JsonPropertyName("caption")] string? Caption,
    [property: JsonPropertyName("items")] IReadOnlyList<string>? Items = null);
