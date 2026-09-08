namespace TelegramAi.Backend.Api.Contracts.Answers;

public sealed record CreateAnswerResponse(
    string ContentId,
    string Answer,
    IReadOnlyList<int> UsedChunkIndexes,
    string Language,
    string Provider);
