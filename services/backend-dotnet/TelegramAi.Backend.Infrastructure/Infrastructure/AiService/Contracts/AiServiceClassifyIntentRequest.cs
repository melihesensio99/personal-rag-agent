using System.Text.Json.Serialization;

namespace TelegramAi.Backend.Infrastructure.AiService.Contracts;

public sealed record AiServiceClassifyIntentRequest(
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("current_date")] string CurrentDate);
