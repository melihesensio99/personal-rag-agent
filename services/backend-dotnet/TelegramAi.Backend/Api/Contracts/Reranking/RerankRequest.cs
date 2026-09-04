namespace TelegramAi.Backend.Api.Contracts.Reranking;

public sealed record RerankRequest(string Query, IReadOnlyList<RerankDocument> Documents);
