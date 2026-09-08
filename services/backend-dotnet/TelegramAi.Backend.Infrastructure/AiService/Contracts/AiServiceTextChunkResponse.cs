using System.Text.Json.Serialization;

namespace TelegramAi.Backend.Infrastructure.AiService.Contracts;

public sealed record AiServiceTextChunkResponse(
    [property: JsonPropertyName("index")] int Index,
    [property: JsonPropertyName("text")] string Text,
    [property: JsonPropertyName("char_start")] int CharStart,
    [property: JsonPropertyName("char_end")] int CharEnd);
