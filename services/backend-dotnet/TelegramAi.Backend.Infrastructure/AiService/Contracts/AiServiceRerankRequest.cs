using System.Text.Json.Serialization;

namespace TelegramAi.Backend.Infrastructure.AiService.Contracts;

public sealed record AiServiceRerankRequest(
    [property: JsonPropertyName("query")] string Query,
    [property: JsonPropertyName("documents")] IReadOnlyList<AiServiceRerankDocument> Documents);

public sealed record AiServiceRerankDocument(
    [property: JsonPropertyName("index")] int Index,
    [property: JsonPropertyName("text")] string Text);
