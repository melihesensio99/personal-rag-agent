using System.Text.Json.Serialization;

namespace TelegramAi.Backend.Infrastructure.AiService.Contracts;

public sealed record AiServiceRerankResponse(
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("scores")] IReadOnlyList<AiServiceRerankScore> Scores);

public sealed record AiServiceRerankScore(
    [property: JsonPropertyName("index")] int Index,
    [property: JsonPropertyName("score")] double Score);
