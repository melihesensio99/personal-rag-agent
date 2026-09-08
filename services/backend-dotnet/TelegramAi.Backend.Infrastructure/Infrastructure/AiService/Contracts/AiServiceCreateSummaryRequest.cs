using System.Text.Json.Serialization;

namespace TelegramAi.Backend.Infrastructure.AiService.Contracts;

public sealed record AiServiceCreateSummaryRequest(
    [property: JsonPropertyName("content_id")] string ContentId,
    [property: JsonPropertyName("text")] string Text);
