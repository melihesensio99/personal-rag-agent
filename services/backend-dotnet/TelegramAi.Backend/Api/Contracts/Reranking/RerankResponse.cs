namespace TelegramAi.Backend.Api.Contracts.Reranking;

public sealed record RerankResponse(string Model, IReadOnlyList<RerankScore> Scores);
