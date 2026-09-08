namespace TelegramAi.Backend.Application.Contracts.Reranking;
public sealed record RerankDocumentInput(int Index, string Text);
public sealed record RerankInput(string Query, IReadOnlyList<RerankDocumentInput> Documents);
public sealed record RerankScoreResult(int Index, double Score);
public sealed record RerankResult(string Model, IReadOnlyList<RerankScoreResult> Scores);
